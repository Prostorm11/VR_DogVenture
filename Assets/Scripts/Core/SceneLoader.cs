using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRDogVenture.Core
{
    /// <summary>
    /// Simple scene loader - loads scenes by name or index.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        [Header("Scene Settings")]
        [SerializeField] private string gameSceneName = "BasicScene";
        [SerializeField] private string menuSceneName = "SampleScene";
        
        /// <summary>
        /// Load the game scene (BasicScene)
        /// </summary>
        public void LoadGameScene()
        {
            Debug.Log($"[SceneLoader] Loading game scene: {gameSceneName}");
            SceneManager.LoadScene(gameSceneName);
        }
        
        /// <summary>
        /// Load the menu scene (SampleScene)
        /// </summary>
        public void LoadMenuScene()
        {
            Debug.Log($"[SceneLoader] Loading menu scene: {menuSceneName}");
            SceneManager.LoadScene(menuSceneName);
        }
        
        /// <summary>
        /// Load scene by name
        /// </summary>
        public void LoadScene(string sceneName)
        {
            Debug.Log($"[SceneLoader] Loading scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
        
        /// <summary>
        /// Load scene by build index
        /// </summary>
        public void LoadSceneByIndex(int index)
        {
            Debug.Log($"[SceneLoader] Loading scene index: {index}");
            SceneManager.LoadScene(index);
        }
        
        /// <summary>
        /// Reload current scene
        /// </summary>
        public void ReloadCurrentScene()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            Debug.Log($"[SceneLoader] Reloading scene: {currentScene}");
            SceneManager.LoadScene(currentScene);
        }
        
        /// <summary>
        /// Called by PlayButton's XR Simple Interactable SelectEntered event
        /// </summary>
        public void PlayEntered()
        {
            Debug.Log("[SceneLoader] Play button pressed! Loading game...");
            LoadGameScene();
        }
        
        /// <summary>
        /// Quit the application
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[SceneLoader] Quitting game...");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}
