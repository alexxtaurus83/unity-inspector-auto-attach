using UnityEditor;
using UnityEngine;
using System.Text;

namespace AttachAttributes {
    public static class CopyPathMenuItem {
        [MenuItem("GameObject/Copy Hierarchy Path", false, 10)] 
        private static void CopyPath() {
            var go = Selection.activeGameObject;
            if (go == null) {
                return;
            }            
            string path = GetInScenePath(go.transform);
            EditorGUIUtility.systemCopyBuffer = path;           
        }        
        [MenuItem("GameObject/Copy Hierarchy Path", true)]
        private static bool CopyPathValidation() {
            return Selection.gameObjects.Length == 1;
        }        
        private static string GetInScenePath(Transform transform) {
            var current = transform;
            var inScenePath = new System.Collections.Generic.List<string> { current.name };
            while (current.parent != null) {
                current = current.parent;
                inScenePath.Add(current.name);
           }         
            inScenePath.Reverse();
            var sb = new StringBuilder();
            foreach (var item in inScenePath)
                sb.Append($"/{item}");
            return sb.ToString();
        }
    }
}