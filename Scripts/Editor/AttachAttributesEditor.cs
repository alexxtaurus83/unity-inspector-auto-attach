using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AttachAttributes {
    public static class AttachAttributesUtils {
        private const string k_ContextMenuItemLabel = "CONTEXT/Component/AttachAttributes";
        private const string k_ToolsMenuItemLabel = "Tools/Evial/AttachAttributes";
        private const string k_ForceReattachMenuItemLabel = "CONTEXT/Component/Force Re-attach Attributes";

        private const string k_EditorPrefsAttachAttributesGlobal = "IsAttachAttributesActive";

        public static bool IsEnabled {
            get => EditorPrefs.GetBool(k_EditorPrefsAttachAttributesGlobal, true);
            set {
                if (value) EditorPrefs.DeleteKey(k_EditorPrefsAttachAttributesGlobal);
                else
                    EditorPrefs.SetBool(k_EditorPrefsAttachAttributesGlobal,
                        value); // clear value if it's equals defaultValue
            }
        }

        [MenuItem(k_ContextMenuItemLabel)]
        [MenuItem(k_ToolsMenuItemLabel)]
        private static void ToggleAction() {
            IsEnabled = !IsEnabled;
        }

        [MenuItem(k_ContextMenuItemLabel, true)]
        [MenuItem(k_ToolsMenuItemLabel, true)]
        private static bool ToggleActionValidate() {
            Menu.SetChecked(k_ContextMenuItemLabel, IsEnabled);
            Menu.SetChecked(k_ToolsMenuItemLabel, IsEnabled);
            return true;
        }

        [MenuItem(k_ForceReattachMenuItemLabel)]
        public static void ForceReattach(MenuCommand command) {
            var component = command.context as Component;
            if (component == null) return;

            var serializedObject = new SerializedObject(component);
            var iterator = serializedObject.GetIterator();

            // Iterate through all properties to find those with AttachPropertyAttribute
            while (iterator.NextVisible(true)) {
                // Skip the script property as it's not a field in our script
                if (iterator.propertyPath == "m_Script") continue;

                // Get the field info for this property to check for attributes
                var fieldInfo = GetFieldInfoForProperty(component.GetType(), iterator.propertyPath);
                if (fieldInfo != null) {
                    var attachAttribute = fieldInfo.GetCustomAttribute<AttachPropertyAttribute>();
                    if (attachAttribute != null) {
                        // Find the appropriate PropertyDrawer for this attribute type
                        var propertyDrawer = GetPropertyDrawerForAttachAttribute(attachAttribute);
                        if (propertyDrawer != null) {
                            // Call the UpdateProperty method directly to force reattachment
                            propertyDrawer.UpdateProperty(iterator);
                        }
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        // Helper method to get FieldInfo for a property path
        private static System.Reflection.FieldInfo GetFieldInfoForProperty(System.Type componentType, string propertyPath) {
            // Extract the field name from the property path (first part before any dots)
            string fieldName = propertyPath;
            int dotIndex = propertyPath.IndexOf('.');
            if (dotIndex != -1) {
                fieldName = propertyPath.Substring(0, dotIndex);
            }

            // Look for the field in the type hierarchy
            System.Reflection.FieldInfo fieldInfo = null;
            System.Type currentType = componentType;
            while (currentType != null) {
                fieldInfo = currentType.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (fieldInfo != null) break;
                currentType = currentType.BaseType;
            }

            return fieldInfo;
        }

        // Helper method to get the appropriate PropertyDrawer for an attach attribute
        private static AttachAttributePropertyDrawer GetPropertyDrawerForAttachAttribute(AttachPropertyAttribute attribute) {
            System.Type drawerType = null;

            // Determine the appropriate drawer type based on the attribute type
            if (attribute is GetComponentAttribute) {
                drawerType = typeof(GetComponentAttributeEditor);
            } else if (attribute is GetComponentInChildrenAttribute) {
                drawerType = typeof(GetComponentInChildrenAttributeEditor);
            } else if (attribute is GetComponentsInChildrenAttribute) {
                drawerType = typeof(GetComponentsInChildrenAttributeEditor);
            } else if (attribute is AddComponentAttribute) {
                drawerType = typeof(AddComponentAttributeEditor);
            } else if (attribute is GetComponentInParentAttribute) {
                drawerType = typeof(GetComponentInParentAttributeEditor);
            } else if (attribute is GetComponentsInParentAttribute) {
                drawerType = typeof(GetComponentsInParentAttributeEditor);
            } else if (attribute is GetPrefabAttribute) {
                drawerType = typeof(GetPrefabAttributeEditor);
            } else if (attribute is AddComponentAtParentAttribute) {
                drawerType = typeof(AddComponentAtParentAttributeEditor);
            } else if (attribute is GetComponentByPathAttribute) {
                drawerType = typeof(GetComponentByPathAttributeEditor);
            } else if (attribute is GetComponentsByPathAttribute) {
                drawerType = typeof(GetComponentsByPathAttributeEditor);
            }

            if (drawerType != null) {
                var drawer = (AttachAttributePropertyDrawer)System.Activator.CreateInstance(drawerType);
                drawer.SetAttribute(attribute);  // Set the attribute after creation
                return drawer;
            }

            return null;
        }

        public static string GetPropertyType(this SerializedProperty property) {
            var type = property.type;
            // Handle both formats: PPtr<$TypeName> and PPtr<TypeName>
            var match = Regex.Match(type, @"PPtr<\$?(.*?)>");
            if (match.Success)
                type = match.Groups[1].Value;
            return type;
        }

        public static GameObject GetGameObject(this SerializedProperty property) {
            var component = property.serializedObject.targetObject as Component;
            return component != null ? component.gameObject : null;
        }

        public static Type GetComponentType(this SerializedProperty property) {
            return property.GetPropertyType().StringToComponentType();
        }

        private static readonly FieldInfo s_CustomPropertyDrawerTypeField =
            typeof(CustomPropertyDrawer).GetField("m_Type", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo s_CustomPropertyDrawerUseForChildrenField =
            typeof(CustomPropertyDrawer).GetField("m_UseForChildren",
                BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool TargetsType(this CustomPropertyDrawer cpd, Type type) {
            var cpdType = (Type)s_CustomPropertyDrawerTypeField.GetValue(cpd);
            var useForChildren = (bool)s_CustomPropertyDrawerUseForChildrenField.GetValue(cpd);
            return useForChildren ? type.IsSubclassOf(cpdType) : cpdType == type;
        }


        private static readonly TypeCache.TypeCollection s_AllComponentTypes = TypeCache.GetTypesDerivedFrom<Component>();
        private static readonly TypeCache.TypeCollection s_AllPropertyDrawers = TypeCache.GetTypesWithAttribute<CustomPropertyDrawer>();
        private static readonly TypeCache.TypeCollection s_SerializableTypes = TypeCache.GetTypesWithAttribute<SerializableAttribute>();

        // Legacy approach using reflection (kept for compatibility with non-TypeCache types if needed)
        private static Type[] GetAllTypesLegacy() {
            return System.AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).ToArray();
        }


        private static readonly Dictionary<string, Type> s_ComponentTypeCache = s_AllComponentTypes
            .GroupBy(t => t.FullName)
            .ToDictionary(g => g.Key, g => g.First());

        public static Type StringToComponentType(this string aClassName) {
            // Try exact match first for performance
            if (s_ComponentTypeCache.TryGetValue(aClassName, out Type type)) {
                return type;
            }
            
            // Fallback to case-insensitive search by FullName
            foreach(var kvp in s_ComponentTypeCache) {
                if (string.Equals(kvp.Key, aClassName, StringComparison.OrdinalIgnoreCase)) {
                    return kvp.Value;
                }
            }
            
            // Additional fallback: if the input is just a class name (not full name),
            // search for types with matching class name
            foreach(var kvp in s_ComponentTypeCache) {
                if (string.Equals(kvp.Value.Name, aClassName, StringComparison.OrdinalIgnoreCase)) {
                    return kvp.Value;
                }
            }
            
            return null;
        }

        private static readonly Dictionary<string, PropertyDrawer> s_PropertyDrawers = new Dictionary<string, PropertyDrawer>();

        private static PropertyDrawer GetPropertyDrawerForProperty(SerializedProperty prop) {
            // Use both propertyPath and target object InstanceID to create a key
            // Note: When arrays are reordered, propertyPath changes, so we'll get a new key and new drawer instance
            string key = prop.propertyPath + "_" + prop.serializedObject.targetObject.GetInstanceID();

            if (!s_PropertyDrawers.TryGetValue(key, out var drawer)) {
                var propertyTypeStr = prop.GetPropertyType();
                var propertyType = s_SerializableTypes.FirstOrDefault(x => x.Name == propertyTypeStr);
                if (propertyType == null) return null; // Handle case where type isn't found

                try {
                    Type drawerType = s_AllPropertyDrawers.First(x =>
                        x.GetCustomAttributes<CustomPropertyDrawer>().Any(cpd => cpd.TargetsType(propertyType)));

                    drawer = (PropertyDrawer)Activator.CreateInstance(drawerType);
                } catch (InvalidOperationException e) {
                    drawer = null;
                }

                s_PropertyDrawers[key] = drawer;
            }

            return drawer;
        }

        public static void DefaultPropertyGUI(Rect pos, SerializedProperty prop, GUIContent label, bool includeChildren) {
            PropertyDrawer drawer = GetPropertyDrawerForProperty(prop);

            if (drawer != null) {
                drawer.OnGUI(pos, prop, label);
            } else {
                EditorGUI.PropertyField(pos, prop, label, includeChildren);
            }
        }

        public static float GetDefaultPropertyGUIHeight(SerializedProperty prop, GUIContent label) {
            PropertyDrawer drawer = GetPropertyDrawerForProperty(prop);

            if (drawer != null) {
                return drawer.GetPropertyHeight(prop, label);
            } else {
                return EditorGUI.GetPropertyHeight(prop, label, true);
            }
        }


        public static Type StringToType(this string aClassName) {
            try {
                // Use the case-insensitive StringToComponentType method
                return StringToComponentType(aClassName);
            } catch {
                return null;
            }
        }

        public static void ApplyToAllTargets(SerializedProperty property, System.Action<GameObject, Type> action) {
            if (property.serializedObject.targetObjects.Length <= 1) {
                // Single object case - existing behavior
                var targetObj = property.serializedObject.targetObject;
                if (targetObj is MonoBehaviour monoBehaviour) {
                    var go = monoBehaviour.gameObject;
                    var type = property.GetPropertyType().StringToType();
                    if (type != null) {
                        action(go, type);
                    }
                }
            } else {
                // Multi-object case
                foreach (var targetObject in property.serializedObject.targetObjects) {
                    if (targetObject is MonoBehaviour monoBehaviour) {
                        var go = monoBehaviour.gameObject;
                        var type = property.GetPropertyType().StringToType();
                        if (type != null) {
                            action(go, type);
                        }
                    }
                }
            }
        }

        // Undo functionality removed as per requirements

        public static GameObject FindGameObjectByName(GameObject root, string name, bool includeInactive) {
            if (root.name.Equals(name, StringComparison.Ordinal)) { 
                return root; 
            }            
            return FindGameObjectsByName(root, name, includeInactive).FirstOrDefault();
        }

        public static GameObject FindGameObjectInChildren(GameObject root, string name, bool includeInactive) {
            var transforms = root.GetComponentsInChildren<Transform>(includeInactive);
            foreach (var transform in transforms) {
                if (transform.name.Equals(name, StringComparison.Ordinal) && transform != root.transform) { 
                    return transform.gameObject;
                }
            }
            return null;
        }        

        public static GameObject FindGameObjectInScene(string name, bool includeInactive) {
            var allObjects = FindGameObjectsInScene(name, includeInactive);
            return allObjects.FirstOrDefault();
        }

        public static string GetFieldName(SerializedProperty property) {
            // Extract the field name from the property path
            string path = property.propertyPath;
            int dotIndex = path.IndexOf('.');
            if (dotIndex != -1)
                path = path.Substring(0, dotIndex);
            return path;
        }

        public static List<GameObject> FindGameObjectsByName(GameObject root, string name, bool includeInactive) {
            var transforms = root.GetComponentsInChildren<Transform>(includeInactive);
            var result = new List<GameObject>();
            foreach (var t in transforms) {
                if (t.name.Equals(name, StringComparison.Ordinal)) {
                    result.Add(t.gameObject);
                }
            }
            return result;
        }

        public static GameObject FindGameObjectInParent(GameObject root, string name, bool includeInactive) {
            List<GameObject> objects = FindGameObjectsInParent(root, name, includeInactive);
            if (objects.Count > 0 ) {
                return objects[0];
            } else {
                return null;
            }
        }

        public static List<GameObject> FindGameObjectsInParent(GameObject root, string name, bool includeInactive) {
            List<GameObject> foundObjects = new List<GameObject>();
            Transform parentTranform = root.transform.parent;
            if (parentTranform != null) {             
                if (parentTranform.name.Equals(name, StringComparison.Ordinal)) {
                    if (includeInactive || parentTranform.gameObject.activeInHierarchy)
                        foundObjects.Add(parentTranform.gameObject);
                }
                // Also check direct children of the parent
                for (int i = 0; i < parentTranform.childCount; i++) {
                    Transform child = parentTranform.GetChild(i);
                    if (child.name.Equals(name, StringComparison.Ordinal)) {
                        if (includeInactive || child.gameObject.activeInHierarchy)
                            foundObjects.Add(child.gameObject);
                    }
                }
            }
            return foundObjects;
        }

        public static List<GameObject> FindGameObjectsInScene(string name, bool includeInactive) {
            var allRootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            var foundObjects = new List<GameObject>();

            foreach (var rootObj in allRootObjects) {
                var transforms = rootObj.GetComponentsInChildren<Transform>(includeInactive);
                foreach (var t in transforms) {
                    if (t.name.Equals(name, StringComparison.Ordinal)) {
                        foundObjects.Add(t.gameObject);
                    }
                }
            }

            return foundObjects;
        }

        public static Type GetElementType(Type collectionType) {
            if (collectionType.IsArray)
                return collectionType.GetElementType();
            if (collectionType.IsGenericType && collectionType.GetGenericArguments().Length > 0)
                return collectionType.GetGenericArguments()[0];
            return null;
        }

        public static bool IsCollectionType(Type type) {
            return type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>));
        }

        /// <summary>
        /// Shared method to update array property from GameObject list
        /// </summary>
        public static void UpdateArrayPropertyFromGameObjects(SerializedProperty property, List<GameObject> gameObjects, Type type) {
            // Collect the components first
            var newComponents = new List<Component>();
            foreach (var go in gameObjects) {
                var comp = go.GetComponent(type);
                if (comp != null) newComponents.Add(comp);
            }

            // Check if distinct lists are actually different to avoid constant dirtying
            bool isDifferent = property.arraySize != newComponents.Count;
            if (!isDifferent) {
                for (int i = 0; i < property.arraySize; i++) {
                    if (property.GetArrayElementAtIndex(i).objectReferenceValue != newComponents[i]) {
                        isDifferent = true;
                        break;
                    }
                }
            }

            if (isDifferent) {
                property.arraySize = newComponents.Count;
                for (int i = 0; i < newComponents.Count; i++) {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = newComponents[i];
                }
            }
        }
    }

    /// Base class for Attach Attribute
    public class AttachAttributePropertyDrawer : PropertyDrawer {
        private Color m_GUIColorDefault = new Color(.6f, .6f, .6f, 1);
        private Color m_GUIColorNull = new Color(1f, .5f, .5f, 1);

        // Instance-based cache to store failed lookups to avoid repeated expensive searches
        // Key: property cache key, Value: (timestamp when cached, bool indicating failure)
        private Dictionary<string, (double timestamp, bool failed)> m_FailedLookups = new Dictionary<string, (double timestamp, bool failed)>();
        // Retry failed lookups after this interval (in seconds)
        private const double k_RetryInterval = 2.0;

        // Allow external setting of the attribute for manual instantiation
        protected AttachPropertyAttribute m_ExternalAttribute;

        // Generate unique cache key for a property
        private string GetCacheKey(SerializedProperty property) {
            return $"{property.propertyPath}_{property.serializedObject.targetObject.GetInstanceID()}";
        }

        /// Setter for external attribute assignment
        public void SetAttribute(AttachPropertyAttribute attr) {
            m_ExternalAttribute = attr;
            UnityEngine.Debug.Log($"[AttachAttributes] Set external attribute: {attr?.GetType().Name ?? "null"}");
        }

        /// Getter that prioritizes external attribute if available, otherwise uses Unity's internal one
        protected AttachPropertyAttribute GetEffectiveAttribute() {
            var effectiveAttr = m_ExternalAttribute ?? (AttachPropertyAttribute)attribute;
            UnityEngine.Debug.Log($"[AttachAttributes] Using attribute: {effectiveAttr?.GetType().Name ?? "null"} (external: {m_ExternalAttribute != null}, internal: {attribute != null})");
            return effectiveAttr;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            // turn off attribute if not active or in Play Mode (imitate as build will works)
            bool attachAttributeEnabled = AttachAttributesUtils.IsEnabled && !Application.isPlaying;
            using (new EditorGUI.DisabledScope(!attachAttributeEnabled)) {
                AttachAttributesUtils.DefaultPropertyGUI(position, property, label, true);
                // Check if we should update based on domain reload or if property should be updated
                if (attachAttributeEnabled && (ShouldUpdatePropertyAfterDomainReload(property) || ShouldUpdateProperty(property))) {
                    // Mark that we've processed after domain reload
                    if (s_AfterDomainReload && !s_HasProcessedAfterReload) {
                        s_HasProcessedAfterReload = true;
                    }
                    
                    // Defer expensive operations to avoid GUI event issues
                    // Use EditorApplication.delayCall to avoid modifying properties during Layout event
                    EditorApplication.delayCall += () => {
                        // Must check if property is still valid before updating
                        if (property != null && property.serializedObject != null) {
                            property.serializedObject.Update(); // Ensure we have latest data
                            UpdateProperty(property);
                            property.serializedObject.ApplyModifiedProperties(); // Apply changes
                        }
                    };
                }
            }

            EditorGUI.EndProperty();
        }

        /// Customize it for each attribute
        public virtual void UpdateProperty(SerializedProperty property) {
            // Base implementation does nothing - derived classes should override
        }

        /// Can be customized per attribute
        public virtual bool ShouldUpdateProperty(SerializedProperty property) {
            // Only update if the value is null and we haven't cached a failed lookup recently
            string cacheKey = GetCacheKey(property);

            if (m_FailedLookups.TryGetValue(cacheKey, out var cachedData)) {
                // Check if enough time has passed to retry
                if ((EditorApplication.timeSinceStartup - cachedData.timestamp) < k_RetryInterval) {
                    return false; // Skip expensive operations if recently failed
                } else {
                    // Remove the expired entry to allow retry
                    m_FailedLookups.Remove(cacheKey);
                }
            }

            return property.objectReferenceValue == null;
        }

        // Method to mark a property as having a failed lookup
        protected void MarkFailedLookup(SerializedProperty property) {
            string cacheKey = GetCacheKey(property);
            m_FailedLookups[cacheKey] = (EditorApplication.timeSinceStartup, true);
        }

        // Method to clear failed lookup cache for a property
        protected void ClearFailedLookup(SerializedProperty property) {
            string cacheKey = GetCacheKey(property);
            if (m_FailedLookups.ContainsKey(cacheKey))
                m_FailedLookups.Remove(cacheKey);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return AttachAttributesUtils.GetDefaultPropertyGUIHeight(property, label);
        }

        // Static variables to track domain reload state
        private static bool s_AfterDomainReload = true; // Start with true to ensure initial update
        private static bool s_HasProcessedAfterReload = false;

        /// <summary>
        /// Determines if property should update after domain reload (script changes)
        /// </summary>
        public virtual bool ShouldUpdatePropertyAfterDomainReload(SerializedProperty property) {
            // Check if we're processing after domain reload and this hasn't been processed yet
            if (s_AfterDomainReload && !s_HasProcessedAfterReload) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resets the domain reload state for all instances
        /// </summary>
        public static void ResetDomainReloadState() {
            s_AfterDomainReload = true;
            s_HasProcessedAfterReload = false;
        }

        /// <summary>
        /// Handles the domain reload event to reset the state appropriately
        /// </summary>
        [InitializeOnLoadMethod]
        private static void SubscribeToDomainReload() {
            // Use EditorApplication.update to periodically check if we're past the initial reload period
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate() {
            // After domain reload, we want to reset the flag after a short delay
            // to ensure all editors have had a chance to update
            if (s_AfterDomainReload && s_HasProcessedAfterReload) {
                s_AfterDomainReload = false;
            }
        }
    }


    /// GetComponent
    [CustomPropertyDrawer(typeof(GetComponentAttribute))]
    public class GetComponentAttributeEditor : AttachAttributePropertyDrawer {
        public override void UpdateProperty(SerializedProperty property) {
            var type = property.GetComponentType();
            var go = property.GetGameObject();

            property.objectReferenceValue = go.GetComponent(type);
        }
    }

    /// GetComponentInChildren
    [CustomPropertyDrawer(typeof(GetComponentInChildrenAttribute))]
    public class GetComponentInChildrenAttributeEditor : AttachAttributePropertyDrawer {
        public override void UpdateProperty(SerializedProperty property) {
            var type = property.GetComponentType();
            var go = property.GetGameObject();

            GetComponentInChildrenAttribute labelAttribute = (GetComponentInChildrenAttribute)GetEffectiveAttribute();
            GameObject targetObject = null;
            var targetName = labelAttribute.GameObjectName;
            if (string.IsNullOrEmpty(targetName)) {
                targetName = AttachAttributesUtils.GetFieldName(property);                
            }
            targetObject = AttachAttributesUtils.FindGameObjectByName(go, targetName, labelAttribute.IncludeInactive);

            if (targetObject != null) {
                var component = targetObject.GetComponent(type);
                if (component != null) {
                    property.objectReferenceValue = component;
                    ClearFailedLookup(property); // Clear cache since we found the component
                } else {
                    MarkFailedLookup(property); // Cache that we couldn't find the component
                }
            } else {
                MarkFailedLookup(property); // Cache that we couldn't find the GameObject
            }
        }
    }

    /// GetComponentsInChildren
    [CustomPropertyDrawer(typeof(GetComponentsInChildrenAttribute))]
    public class GetComponentsInChildrenAttributeEditor : AttachAttributePropertyDrawer {
        public override void UpdateProperty(SerializedProperty property) {
            var type = property.GetComponentType();
            var go = property.GetGameObject();
            if (go == null) return;

            var labelAttribute = (GetComponentsInChildrenAttribute)GetEffectiveAttribute();

            if (!string.IsNullOrEmpty(labelAttribute.GameObjectName)) {
                // Use custom name-based search instead of slow transform.Find()
                var child = AttachAttributesUtils.FindGameObjectByName(go, labelAttribute.GameObjectName, labelAttribute.IncludeInactive);
                if (!child) {
                    MarkFailedLookup(property); // Cache that we couldn't find the child
                    return;
                }
                UpdateArrayProperty(property, child, type, labelAttribute.IncludeInactive);
            } else {
                // Use name-based search
                List<GameObject> targetObjects = new List<GameObject>();

                if (labelAttribute.GameObjectNames != null && labelAttribute.GameObjectNames.Length > 0) {
                    // Search for each specified name
                    foreach (string targetName in labelAttribute.GameObjectNames) {
                        GameObject found = AttachAttributesUtils.FindGameObjectByName(go, targetName, labelAttribute.IncludeInactive);
                        if (found != null)
                            targetObjects.Add(found);
                    }
                } else if (labelAttribute.GameObjectName != null) {
                    // Search for specified name
                    GameObject found = AttachAttributesUtils.FindGameObjectByName(go, labelAttribute.GameObjectName, labelAttribute.IncludeInactive);
                    if (found != null)
                        targetObjects.Add(found);
                } else {
                    // Search for field name
                    string fieldName = AttachAttributesUtils.GetFieldName(property);
                    targetObjects = AttachAttributesUtils.FindGameObjectsByName(go, fieldName, labelAttribute.IncludeInactive);
                }

                if (targetObjects.Count > 0) {
                    AttachAttributesUtils.UpdateArrayPropertyFromGameObjects(property, targetObjects, type);
                } else {
                    MarkFailedLookup(property); // Cache that we couldn't find any GameObjects
                }
            }
        }

        private static void UpdateArrayProperty(SerializedProperty property, GameObject go, Type type,
            bool includeInactive) {
            var componentsInChildren = go.GetComponentsInChildren(type, includeInactive);

            // Collect the components first
            var newComponents = new List<Component>();
            foreach (var component in componentsInChildren) {
                newComponents.Add(component);
            }

            // Check if distinct lists are actually different to avoid constant dirtying
            bool isDifferent = property.arraySize != newComponents.Count;
            if (!isDifferent) {
                for (int i = 0; i < property.arraySize; i++) {
                    if (property.GetArrayElementAtIndex(i).objectReferenceValue != newComponents[i]) {
                        isDifferent = true;
                        break;
                    }
                }
            }

            if (isDifferent) {
                property.arraySize = newComponents.Count;
                for (int i = 0; i < newComponents.Count; i++) {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = newComponents[i];
                }
            }
        }


    }


    /// AddComponent
    [CustomPropertyDrawer(typeof(AddComponentAttribute))]
    public class AddComponentAttributeEditor : AttachAttributePropertyDrawer {
        public override void UpdateProperty(SerializedProperty property) {
            var type = property.GetComponentType();
            var go = property.GetGameObject();
            if (go == null) return;

            var existingComponent = go.GetComponent(type);
            if (existingComponent != null) {
                property.objectReferenceValue = existingComponent;
                return;
            }

            property.objectReferenceValue = go.AddComponent(type);
        }
    }

    /// GetComponentInParent
    [CustomPropertyDrawer(typeof(GetComponentInParentAttribute))]
    public class GetComponentInParentAttributeEditor : AttachAttributePropertyDrawer {
        public override void UpdateProperty(SerializedProperty property) {
            var type = property.GetComponentType();
            var go = property.GetGameObject();
            if (go == null) return;

            GetComponentInParentAttribute labelAttribute = (GetComponentInParentAttribute)GetEffectiveAttribute();

            GameObject targetObject = null;
            string targetName = labelAttribute.GameObjectName;
            if (string.IsNullOrEmpty(targetName)) {
                targetName = AttachAttributesUtils.GetFieldName(property);
            } 
            targetObject = AttachAttributesUtils.FindGameObjectInParent(go, targetName, labelAttribute.IncludeInactive);

            if (targetObject != null) {
                var component = targetObject.GetComponent(type);
                if (component != null) {
                    property.objectReferenceValue = component;
                    ClearFailedLookup(property); // Clear cache since we found the component
                } else {
                    MarkFailedLookup(property); // Cache that we couldn't find the component
                }
            } else {
                MarkFailedLookup(property); // Cache that we couldn't find the GameObject
            }
        }
    }

    /// GetComponentsInParent
    [CustomPropertyDrawer(typeof(GetComponentsInParentAttribute))]
    public class GetComponentsInParentAttributeEditor : AttachAttributePropertyDrawer {
        public override void UpdateProperty(SerializedProperty property) {
            var type = property.GetComponentType();
            var go = property.GetGameObject();
            if (go == null) return;

            var labelAttribute = (GetComponentsInParentAttribute)GetEffectiveAttribute();

            List<GameObject> targetObjects = new List<GameObject>();

            if (labelAttribute.GameObjectNames != null && labelAttribute.GameObjectNames.Length > 0) {
                // Search for each specified name
                foreach (string targetName in labelAttribute.GameObjectNames) {
                    GameObject found = AttachAttributesUtils.FindGameObjectInParent(go, targetName, labelAttribute.IncludeInactive);
                    if (found != null)
                        targetObjects.Add(found);
                }
            } else {
                // Search for field name
                string fieldName = AttachAttributesUtils.GetFieldName(property);
                targetObjects = AttachAttributesUtils.FindGameObjectsInParent(go, fieldName, labelAttribute.IncludeInactive);
            }

            if (targetObjects.Count > 0) {
                AttachAttributesUtils.UpdateArrayPropertyFromGameObjects(property, targetObjects, type);
            } else {
                MarkFailedLookup(property); // Cache that we couldn't find any parent GameObjects
            }
        }

    }

    /// GetPrefab
    [CustomPropertyDrawer(typeof(GetPrefabAttribute))]
    public class GetPrefabAttributeEditor : AttachAttributePropertyDrawer {
        public override void UpdateProperty(SerializedProperty property) {
            GetPrefabAttribute labelAttribute = (GetPrefabAttribute)GetEffectiveAttribute();
            if (labelAttribute.AssetPath != null) {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath(labelAttribute.AssetPath, typeof(GameObject));
                if (!prefab)
                    return;

                property.objectReferenceValue = prefab;
            }
        }
    }

     /// AddComponentAtParent
    [CustomPropertyDrawer(typeof(AddComponentAtParentAttribute))]
    public class AddComponentAtParentAttributeEditor : AttachAttributePropertyDrawer {
        public override void UpdateProperty(SerializedProperty property) {
            var type = property.GetComponentType();
            var go = property.GetGameObject();
            if (go == null) return;

            AddComponentAtParentAttribute labelAttribute = (AddComponentAtParentAttribute)GetEffectiveAttribute();

            Transform parentTransform = go.transform.parent;
            if (parentTransform == null)
                return; // No parent, do nothing

            GameObject targetObject = null;

            if (!string.IsNullOrEmpty(labelAttribute.GameObjectName)) {
                // Find specific named parent
                targetObject = AttachAttributesUtils.FindGameObjectInParent(go, labelAttribute.GameObjectName, labelAttribute.IncludeInactive);
            }

            if (targetObject == null) {
                // Use the immediate parent
                targetObject = parentTransform.gameObject;
            }

            if (targetObject != null) {
                var existingComponent = targetObject.GetComponent(type);
                if (existingComponent != null) {
                    property.objectReferenceValue = existingComponent;
                } else {
                    property.objectReferenceValue = targetObject.AddComponent(type);
                }
            }
        }
    }


    public static class TransformPathHelper {
        public static GameObject FindGameObjectByPath(GameObject root, string path) {
            if (string.IsNullOrEmpty(path) || !path.Contains("/"))
                return null;

            Transform foundTransform = root.transform.Find(path);
            return foundTransform?.gameObject;
        }

        public static List<GameObject> FindGameObjectsByPaths(GameObject root, string[] paths, string fallbackPath = null) {
            List<GameObject> result = new List<GameObject>();

            if (paths != null && paths.Length > 0) {
                foreach (string path in paths) {
                    GameObject go = FindGameObjectByPath(root, path);
                    if (go != null)
                        result.Add(go);
                }
            } else if (!string.IsNullOrEmpty(fallbackPath)) {
                GameObject go = FindGameObjectByPath(root, fallbackPath);
                if (go != null)
                    result.Add(go);
            }

            return result;
        }
    }

    /// GetComponentByPath
    [CustomPropertyDrawer(typeof(GetComponentByPathAttribute))]
    public class GetComponentByPathAttributeEditor : AttachAttributePropertyDrawer {
        public override void UpdateProperty(SerializedProperty property) {
            var type = property.GetComponentType();
            var go = property.GetGameObject();
            if (go == null) return;

            var labelAttribute = (GetComponentByPathAttribute)GetEffectiveAttribute();

            GameObject targetObject = TransformPathHelper.FindGameObjectByPath(go, labelAttribute.path);

            if (targetObject == null || targetObject.GetComponent(type) is not Component component) {
                MarkFailedLookup(property);
                return;
            }

            property.objectReferenceValue = component;
            ClearFailedLookup(property);
        }
    }
    /// GetComponentsByPath
    [CustomPropertyDrawer(typeof(GetComponentsByPathAttribute))]
    public class GetComponentsByPathAttributeEditor : AttachAttributePropertyDrawer {
        public override void UpdateProperty(SerializedProperty property) {
            var type = property.GetComponentType();
            var go = property.GetGameObject();
            if (go == null) return;

            var labelAttribute = (GetComponentsByPathAttribute)GetEffectiveAttribute();

            // Find all target GameObjects
            string fallbackPath = AttachAttributesUtils.GetFieldName(property);
            List<GameObject> targetObjects = TransformPathHelper.FindGameObjectsByPaths(
                go,
                labelAttribute.Paths,
                fallbackPath
            );

            if (targetObjects.Count > 0) {
                AttachAttributesUtils.UpdateArrayPropertyFromGameObjects(property, targetObjects, type);
            } else {
                MarkFailedLookup(property); // Cache that we couldn't find any GameObjects in scene
            }
        }
    }
}