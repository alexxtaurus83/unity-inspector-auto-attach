using System;
using System.Diagnostics;
using UnityEngine;

namespace AttachAttributes {
    /// <summary>
    /// Automatically attaches a component from the same GameObject to the annotated field.
    /// </summary>
    [AttributeUsage(System.AttributeTargets.Field)]
    public class GetComponentAttribute : AttachPropertyAttribute { }

    /// <summary>
    /// Automatically attaches a component from a child GameObject to the annotated field.
    /// </summary>
    [AttributeUsage(System.AttributeTargets.Field)]
    public class GetComponentInChildrenAttribute : AttachPropertyAttribute {
        public GetComponentInChildrenAttribute(bool includeInactive = true) {
            IncludeInactive = includeInactive;
        }

        public GetComponentInChildrenAttribute(string childName) {
            GameObjectName = childName;
        }

        public bool IncludeInactive { get; set; }

        /// <summary>
        /// Name of the GameObject to search for. If not specified, uses the field name as the GameObject name.
        /// </summary>
        public string GameObjectName { get; set; }
    }

    /// <summary>
    /// Automatically populates an array property with components from child GameObjects.
    /// </summary>
    [AttributeUsage(System.AttributeTargets.Field)]
    public class GetComponentsInChildrenAttribute : AttachPropertyAttribute {
        public GetComponentsInChildrenAttribute(bool includeInactive = true) {
            IncludeInactive = includeInactive;
        }

        public GetComponentsInChildrenAttribute(string gameObjectName) {
            GameObjectName = gameObjectName;
        }

        public GetComponentsInChildrenAttribute(string[] gameObjectNames) {
            GameObjectNames = gameObjectNames;
        }

        public bool IncludeInactive { get; set; }

        /// <summary>
        /// Name of the GameObject to search for. If not specified, uses the field name as the GameObject name.
        /// </summary>
        public string GameObjectName { get; set; }

        /// <summary>
        /// Names of the GameObjects to search for. If not specified, uses the field name as the GameObject name.
        /// This parameter takes precedence over GameObjectName if both are provided.
        /// </summary>
        public string[] GameObjectNames { get; set; }
    }

    /// <summary>
    /// Automatically attaches a new component to the same GameObject and attaches it to the annotated field.
    /// </summary>
    [AttributeUsage(System.AttributeTargets.Field)]
    public class AddComponentAttribute : AttachPropertyAttribute { }

    [AttributeUsage(System.AttributeTargets.Field)]
    public class GetComponentInParentAttribute : AttachPropertyAttribute {
        /// <summary>
        /// Name of the parent GameObject to search for. If not specified, uses the field name as the GameObject name.
        /// </summary>
        public string GameObjectName { get; set; }
                
        /// <summary>
        /// Whether to include inactive GameObjects in the search.
        /// </summary>
        public bool IncludeInactive { get; set; } = false;

        public GetComponentInParentAttribute() { }
        public GetComponentInParentAttribute(string gameObjectName) { GameObjectName = gameObjectName; }        
    }

    [AttributeUsage(System.AttributeTargets.Field)]
    public class GetComponentsInParentAttribute : AttachPropertyAttribute {

        public GetComponentsInParentAttribute(bool includeInactive = true) {
            IncludeInactive = includeInactive;
        }

        /// <summary>
        /// Names of the parent GameObjects to search for. If not specified, uses the field name as the GameObject name.
        /// </summary>
        public string[] GameObjectNames { get; set; }

        /// <summary>
        /// Whether to search recursively up the hierarchy or only the immediate parent.
        /// </summary>
        public bool Recursive { get; set; } = true;

        /// <summary>
        /// Whether to include inactive GameObjects in the search.
        /// </summary>
        public bool IncludeInactive { get; set; } = false;
    }

    [AttributeUsage(System.AttributeTargets.Field)]
    public class GetComponentByPathAttribute : AttachPropertyAttribute {
        /// <summary>
        /// Path to the GameObject relative to the current GameObject to search for.
        /// This attribute only performs a search if the path contains a '/'. If not, it does nothing.
        /// Path should contain '/' to indicate hierarchical search (e.g. "Parent/Child/Grandchild").
        /// </summary>
        public string path { get; set; }

        public bool IncludeInactive { get; private set; }

        public GetComponentByPathAttribute(bool includeInactive = true) {
            IncludeInactive = includeInactive;
        }

        public GetComponentByPathAttribute(string path, bool includeInactive = true) {
            this.path = path;
            IncludeInactive = includeInactive;
        }
    }

    [AttributeUsage(System.AttributeTargets.Field)]
    public class GetComponentsByPathAttribute : AttachPropertyAttribute {
        /// <summary>
        /// Paths to the GameObjects relative to the current GameObject to search for.
        /// This attribute only performs a search if a path contains a '/'. If not, it does nothing.
        /// Paths should contain '/' to indicate hierarchical search (e.g. "Parent/Child/Grandchild").
        /// </summary>
        public string[] Paths { get; set; }

        /// <summary>
        /// Whether to include inactive GameObjects in the search.
        /// </summary>
        public bool IncludeInactive { get; set; } = false;
    }

    /// <summary>
    /// Automatically adds a component to the parent GameObject and assigns it to the annotated field.
    /// </summary>
    [AttributeUsage(System.AttributeTargets.Field)]
    public class AddComponentAtParentAttribute : AttachPropertyAttribute {
        /// <summary>
        /// Name of the parent GameObject to add the component to. If not specified, adds to the immediate parent.
        /// </summary>
        public string GameObjectName { get; set; }

        /// <summary>
        /// Whether to include inactive GameObjects in the search.
        /// </summary>
        public bool IncludeInactive { get; set; } = false;
    }

    /// <summary>
    /// Loads a prefab asset and assigns it to the annotated field.
    /// </summary>
    [AttributeUsage(System.AttributeTargets.Field)]
    public class GetPrefabAttribute : AttachPropertyAttribute {
        /// <summary>
        /// Asset path to the prefab to load.
        /// </summary>
        public string AssetPath { get; set; }

        public GetPrefabAttribute(string assetPath) {
            AssetPath = assetPath;
        }
    }

    [Conditional("UNITY_EDITOR")]
    public class AttachPropertyAttribute : PropertyAttribute { }
}