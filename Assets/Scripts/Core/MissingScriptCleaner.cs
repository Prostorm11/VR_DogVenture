using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VRDogVenture.Core
{
    /// <summary>
    /// Editor utility to find and remove missing script references.
    /// Use from menu: Tools → VR DogVenture → Find Missing Scripts
    /// </summary>
    public class MissingScriptCleaner : MonoBehaviour
    {
#if UNITY_EDITOR
        [MenuItem("Tools/VR DogVenture/Find Missing Scripts in Scene")]
        public static void FindMissingScriptsInScene()
        {
            GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int missingCount = 0;
            
            foreach (GameObject go in allObjects)
            {
                Component[] components = go.GetComponents<Component>();
                foreach (Component c in components)
                {
                    if (c == null)
                    {
                        missingCount++;
                        Debug.LogWarning($"Missing script found on: {GetFullPath(go)}", go);
                    }
                }
            }
            
            if (missingCount == 0)
            {
                Debug.Log("[MissingScriptCleaner] No missing scripts found in scene!");
            }
            else
            {
                Debug.LogWarning($"[MissingScriptCleaner] Found {missingCount} missing script(s). Check the warnings above.");
            }
        }
        
        [MenuItem("Tools/VR DogVenture/Remove Missing Scripts from Selected")]
        public static void RemoveMissingScriptsFromSelected()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            
            if (selectedObjects.Length == 0)
            {
                Debug.LogWarning("[MissingScriptCleaner] No objects selected! Select GameObjects first.");
                return;
            }
            
            int totalRemoved = 0;
            
            foreach (GameObject go in selectedObjects)
            {
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                totalRemoved += removed;
                
                if (removed > 0)
                {
                    Debug.Log($"Removed {removed} missing script(s) from: {go.name}");
                    EditorUtility.SetDirty(go);
                }
            }
            
            if (totalRemoved > 0)
            {
                Debug.Log($"[MissingScriptCleaner] Removed {totalRemoved} missing script(s) total. Remember to save the scene!");
            }
            else
            {
                Debug.Log("[MissingScriptCleaner] No missing scripts found on selected objects.");
            }
        }
        
        [MenuItem("Tools/VR DogVenture/Remove ALL Missing Scripts in Scene")]
        public static void RemoveAllMissingScriptsInScene()
        {
            GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int totalRemoved = 0;
            
            foreach (GameObject go in allObjects)
            {
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                totalRemoved += removed;
                
                if (removed > 0)
                {
                    Debug.Log($"Removed {removed} missing script(s) from: {go.name}");
                    EditorUtility.SetDirty(go);
                }
            }
            
            if (totalRemoved > 0)
            {
                Debug.Log($"[MissingScriptCleaner] Removed {totalRemoved} missing script(s) total. Remember to save the scene (Ctrl+S)!");
            }
            else
            {
                Debug.Log("[MissingScriptCleaner] No missing scripts found in scene.");
            }
        }
        
        private static string GetFullPath(GameObject go)
        {
            string path = go.name;
            Transform parent = go.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
#endif
    }
}
