using System;
using System.Linq;
using System.Text.RegularExpressions;
using Dev.Agred.Tools.AttachAttributes;
using UnityEditor;
using UnityEngine;

namespace Dev.Agred.Tools.Editor.AttachAttributes
{
    public static class AttachAttributesUtils
    {
        private const string k_ContextMenuItemLabel = "CONTEXT/Component/AttachAttributes";
        private const string k_ToolsMenuItemLabel = "Tools/Nrjwolf/AttachAttributes";

        private const string k_EditorPrefsAttachAttributesGlobal = "IsAttachAttributesActive";

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(k_EditorPrefsAttachAttributesGlobal, true);
            set
            {
                if (value) EditorPrefs.DeleteKey(k_EditorPrefsAttachAttributesGlobal);
                else
                    EditorPrefs.SetBool(k_EditorPrefsAttachAttributesGlobal,
                        value); // clear value if it's equals defaultValue
            }
        }

        [MenuItem(k_ContextMenuItemLabel)]
        [MenuItem(k_ToolsMenuItemLabel)]
        private static void ToggleAction()
        {
            IsEnabled = !IsEnabled;
        }

        [MenuItem(k_ContextMenuItemLabel, true)]
        [MenuItem(k_ToolsMenuItemLabel, true)]
        private static bool ToggleActionValidate()
        {
            Menu.SetChecked(k_ContextMenuItemLabel, IsEnabled);
            Menu.SetChecked(k_ToolsMenuItemLabel, IsEnabled);
            return true;
        }

        public static string GetPropertyType(this SerializedProperty property)
        {
            var type = property.type;
            var match = Regex.Match(type, @"PPtr<\$(.*?)>");
            if (match.Success)
                type = match.Groups[1].Value;
            return type;
        }

        public static Type StringToType(this string aClassName) => System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes()).First(x => x.IsSubclassOf(typeof(Component)) && x.Name == aClassName);
    }

    /// Base class for Attach Attribute
    public class AttachAttributePropertyDrawer : PropertyDrawer
    {
        private Color m_GUIColorDefault = new Color(.6f, .6f, .6f, 1);
        private Color m_GUIColorNull = new Color(1f, .5f, .5f, 1);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // turn off attribute if not active or in Play Mode (imitate as build will works)
            if (!AttachAttributesUtils.IsEnabled || Application.isPlaying)
            {
                property.serializedObject.Update();
                EditorGUI.PropertyField(position, property, label, true);
                property.serializedObject.ApplyModifiedProperties();
                return;
            }

            bool isPropertyValueNull = property.objectReferenceValue == null;

            // Change GUI color
            var prevColor = GUI.color;
            GUI.color = isPropertyValueNull ? m_GUIColorNull : m_GUIColorDefault;

            // Default draw
            EditorGUI.PropertyField(position, property, label, true);

            // Get property type and GameObject
            property.serializedObject.Update();
            if (isPropertyValueNull)
            {
                var type = property.GetPropertyType().StringToType();
                var go = ((MonoBehaviour) (property.serializedObject.targetObject)).gameObject;
                UpdateProperty(property, go, type);
            }

            property.serializedObject.ApplyModifiedProperties();
            GUI.color = prevColor;
        }

        /// Customize it for each attribute
        public virtual void UpdateProperty(SerializedProperty property, GameObject go, Type type)
        {
            // Do whatever
            // For example to get component 
            // property.objectReferenceValue = go.GetComponent(type);
        }
    }

    #region Attribute Editors

    /// GetComponent
    [CustomPropertyDrawer(typeof(GetComponentAttribute))]
    public class GetComponentAttributeEditor : AttachAttributePropertyDrawer
    {
        public override void UpdateProperty(SerializedProperty property, GameObject go, Type type)
        {
            property.objectReferenceValue = go.GetComponent(type);
        }
    }

    /// GetComponentInChildren
    [CustomPropertyDrawer(typeof(GetComponentInChildrenAttribute))]
    public class GetComponentInChildrenAttributeEditor : AttachAttributePropertyDrawer
    {
        public override void UpdateProperty(SerializedProperty property, GameObject go, Type type)
        {
            var labelAttribute = (GetComponentInChildrenAttribute) attribute;
            if (labelAttribute.ChildName == null)
            {
                property.objectReferenceValue = go.GetComponentInChildren(type, labelAttribute.IncludeInactive);
            }
            else
            {
                var child = go.transform.Find(labelAttribute.ChildName);
                if (child)
                {
                    property.objectReferenceValue = child.GetComponent(type);
                }
            }
        }
    }

    /// GetComponentsInChildren
    [CustomPropertyDrawer(typeof(GetComponentsInChildrenAttribute))]
    public class GetComponentsInChildrenAttributeEditor : AttachAttributePropertyDrawer
    {
        public override void UpdateProperty(SerializedProperty property, GameObject go, Type type)
        {
            var labelAttribute = (GetComponentsInChildrenAttribute) attribute;
            if (labelAttribute.PropertyName == null)
            {
                Debug.LogError("PropertyName parameter in GetComponentsInChildrenAttribute is required!");
                return;
            }

            var arrayProperty = property.serializedObject.FindProperty(labelAttribute.PropertyName);
            if (labelAttribute.ChildName == null)
            {
                UpdateArrayProperty(arrayProperty, go, type, labelAttribute.IncludeInactive);
            }
            else
            {
                var child = go.transform.Find(labelAttribute.ChildName);
                if (!child)
                    return;

                UpdateArrayProperty(arrayProperty, child.gameObject, type, labelAttribute.IncludeInactive);
            }
        }

        private static void UpdateArrayProperty(SerializedProperty property, GameObject go, Type type,
            bool includeInactive)
        {
            var componentsInChildren = go.GetComponentsInChildren(type, includeInactive);
            if (property.arraySize != componentsInChildren.Length)
                property.ClearArray();

            for (var i = 0; i < componentsInChildren.Length; i++)
            {
                var component = componentsInChildren[i];

                var componentAlreadyInArray = false;
                for (var j = 0; j < property.arraySize; j++)
                {
                    var arrayElement = property.GetArrayElementAtIndex(j);
                    if (arrayElement != null && arrayElement.objectReferenceValue == component)
                    {
                        componentAlreadyInArray = true;
                        break;
                    }
                }

                if (componentAlreadyInArray)
                    continue;

                property.InsertArrayElementAtIndex(i);
                property.GetArrayElementAtIndex(i).objectReferenceValue = component;
            }
        }
    }


    /// AddComponent
    [CustomPropertyDrawer(typeof(AddComponentAttribute))]
    public class AddComponentAttributeEditor : AttachAttributePropertyDrawer
    {
        public override void UpdateProperty(SerializedProperty property, GameObject go, Type type)
        {
            property.objectReferenceValue = go.AddComponent(type);
        }
    }

    /// FindObjectOfType
    [CustomPropertyDrawer(typeof(FindObjectOfTypeAttribute))]
    public class FindObjectOfTypeAttributeEditor : AttachAttributePropertyDrawer
    {
        public override void UpdateProperty(SerializedProperty property, GameObject go, Type type)
        {
            property.objectReferenceValue = FindObjectsOfTypeByName(property.GetPropertyType());
        }

        public UnityEngine.Object FindObjectsOfTypeByName(string aClassName)
        {

             var findOption = ((FindObjectOfTypeAttribute)attribute).IncludeInactive
                ? FindObjectsInactive.Include
                : FindObjectsInactive.Exclude;

            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var types = assemblies[i].GetTypes();
                for (int n = 0; n < types.Length; n++)
                {
                     if (typeof(UnityEngine.Object).IsAssignableFrom(types[n]) && aClassName == types[n].Name)
                        return UnityEngine.Object.FindObjectOfType(types[n]);
                        return UnityEngine.Object.FindFirstObjectByType(types[n], findOption);
                }
            }

            return new UnityEngine.Object();
        }
    }

    /// GetComponentInParent
    [CustomPropertyDrawer(typeof(GetComponentInParentAttribute))]
    public class GetComponentInParentAttributeEditor : AttachAttributePropertyDrawer
    {
        public override void UpdateProperty(SerializedProperty property, GameObject go, Type type)
        {
            if (go.transform.parent)
                //property.objectReferenceValue = go.transform.parent.gameObject.GetComponent(type);
                property.objectReferenceValue = go.transform.GetComponentInParent(type);
        }
    }

    /// GetComponentsInParent
    [CustomPropertyDrawer(typeof(GetComponentsInParentAttribute))]
    public class GetComponentsInParentAttributeEditor : AttachAttributePropertyDrawer
    {
        public override void UpdateProperty(SerializedProperty property, GameObject go, Type type)
        {
            if (!go.transform.parent)
                return;

            var labelAttribute = (GetComponentsInParentAttribute) attribute;
            if (labelAttribute.PropertyName == null)
            {
                Debug.LogError("PropertyName parameter in GetComponentsInParentAttribute is required!");
                return;
            }

            var arrayProperty = property.serializedObject.FindProperty(labelAttribute.PropertyName);
            UpdateArrayProperty(arrayProperty, go, type);
        }

        private static void UpdateArrayProperty(SerializedProperty property, GameObject go, Type type)
        {
            var componentsInChildren = go.GetComponentsInChildren(type);
            if (property.arraySize != componentsInChildren.Length)
                property.ClearArray();

            for (var i = 0; i < componentsInChildren.Length; i++)
            {
                var component = componentsInChildren[i];

                var componentAlreadyInArray = false;
                for (var j = 0; j < property.arraySize; j++)
                {
                    var arrayElement = property.GetArrayElementAtIndex(j);
                    if (arrayElement != null && arrayElement.objectReferenceValue == component)
                    {
                        componentAlreadyInArray = true;
                        break;
                    }
                }

                if (componentAlreadyInArray)
                    continue;

                property.InsertArrayElementAtIndex(i);
                property.GetArrayElementAtIndex(i).objectReferenceValue = component;
            }
        }
        
        /// GetPrefab
        [CustomPropertyDrawer(typeof(GetPrefabAttribute))]
        public class GetPrefabAttributeEditor : AttachAttributePropertyDrawer
        {
            public override void UpdateProperty(SerializedProperty property, GameObject go, Type type)
            {
                GetPrefabAttribute labelAttribute = (GetPrefabAttribute) attribute;
                if (labelAttribute.Path != null)
                {
                    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath(labelAttribute.Path, typeof(GameObject));
                    if (!prefab)
                        return;

                    property.objectReferenceValue = prefab;
                }
            }
        }        
    }

    [CustomPropertyDrawer(typeof(BaseCustomFetchAttribute), useForChildren: true)]
    public class CustomFetchAttributeEditor : AttachAttributePropertyDrawer
    {
        public override void UpdateProperty(SerializedProperty property)
        {
            BaseCustomFetchAttribute fetchAttribute = (BaseCustomFetchAttribute)attribute;
            var methodInfo = AttachAttributesUtils.GetFetchMethod(property, fetchAttribute);

            if (methodInfo == null)
            {
                EditorGUILayout.HelpBox($"Unable to find method \"{fetchAttribute.CustomFuncName}\"; ensure the method is static that returns nothing and takes in a \"{nameof(SerializedProperty)}\" for the first parameter and \"{fetchAttribute.GetType().Name}\" as the second parameter.", MessageType.Error);
            }
            else
            {
                methodInfo.Invoke(null, new object[] { property, fetchAttribute });
            }
        }

        public override bool ShouldUpdateProperty(SerializedProperty property)
        {
            BaseCustomFetchAttribute fetchAttribute = (BaseCustomFetchAttribute)attribute;
            if (string.IsNullOrEmpty(fetchAttribute.CustomValidationFuncName))
            {
                return base.ShouldUpdateProperty(property);
            }

            var methodInfo = AttachAttributesUtils.GetFetchValidationMethod(property, fetchAttribute);
            bool ret = false;

            if (methodInfo == null)
            {
                EditorGUILayout.HelpBox($"Unable to find method \"{fetchAttribute.CustomValidationFuncName}\"; ensure the method is static that returns a boolean and takes in a \"{nameof(SerializedProperty)}\" for the first parameter and \"{fetchAttribute.GetType().Name}\" as the second parameter.", MessageType.Error);
            }
            else
            {
                ret = (bool)methodInfo.Invoke(null, new object[] { property, fetchAttribute });
            }

            return ret;
        }
    }
}