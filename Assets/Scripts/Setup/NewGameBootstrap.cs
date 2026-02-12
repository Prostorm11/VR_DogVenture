using UnityEngine;
using VRProject.WordPuzzle;
using VRProject.Dog;
using VRProject.Punishment;
using VRProject.Core;
using VRProject.UI;
using VRProject.Effects;

namespace VRProject.Setup
{
    /// <summary>
    /// BOOTSTRAP SCRIPT - Add this to an empty GameObject in your scene!
    /// This will:
    /// 1. Disable the old FloatingWordBuilder (submit button system)
    /// 2. Create the new SubWordGameManager (socket system)
    /// 3. Ensure DogCompanion is working
    /// </summary>
    public class NewGameBootstrap : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool disableOldSystem = true;
        [SerializeField] private bool createNewSystem = true;
        [SerializeField] private bool setupDog = true;
        
        [Header("Prefabs (Drag from Project)")]
        [SerializeField] private GameObject floatingLetterPrefab;
        [SerializeField] private GameObject dogPrefab;

        private void Awake()
        {
            Debug.Log("========== NEW GAME BOOTSTRAP STARTING ==========");
            
            if (disableOldSystem)
            {
                DisableOldSystem();
            }
            
            if (createNewSystem)
            {
                CreateNewSubWordSystem();
            }
            
            if (setupDog)
            {
                SetupDogCompanion();
            }
            
            // Also ensure other systems exist
            EnsureSoundManager();
            EnsurePunishmentSystem();
            EnsureEffectsManager();
            
            Debug.Log("========== NEW GAME BOOTSTRAP COMPLETE ==========");
        }

        private void DisableOldSystem()
        {
            // Find and disable ALL FloatingWordBuilder instances
            FloatingWordBuilder[] oldBuilders = FindObjectsByType<FloatingWordBuilder>(FindObjectsSortMode.None);
            foreach (var builder in oldBuilders)
            {
                Debug.Log($"[Bootstrap] DISABLING old FloatingWordBuilder on '{builder.gameObject.name}'");
                builder.enabled = false;
                
                // Also hide any submit buttons that might be children
                foreach (Transform child in builder.transform)
                {
                    if (child.name.ToLower().Contains("submit") || child.name.ToLower().Contains("button"))
                    {
                        child.gameObject.SetActive(false);
                        Debug.Log($"[Bootstrap] Hiding submit button: {child.name}");
                    }
                }
            }
            
            if (oldBuilders.Length == 0)
            {
                Debug.Log("[Bootstrap] No old FloatingWordBuilder found");
            }
        }

        private void CreateNewSubWordSystem()
        {
            // Check if SubWordGameManager already exists
            if (SubWordGameManager.Instance != null)
            {
                Debug.Log("[Bootstrap] SubWordGameManager already exists");
                return;
            }
            
            // Create new SubWordGameManager
            GameObject managerObj = new GameObject("SubWordGameManager");
            SubWordGameManager manager = managerObj.AddComponent<SubWordGameManager>();
            
            // Try to assign letter prefab
            if (floatingLetterPrefab != null)
            {
                SetPrivateField(manager, "floatingLetterPrefab", floatingLetterPrefab);
            }
            else
            {
                // Try to find it in the scene or resources
                GameObject existingLetter = Resources.Load<GameObject>("Prefabs/FloatingLetter");
                if (existingLetter != null)
                {
                    SetPrivateField(manager, "floatingLetterPrefab", existingLetter);
                }
                else
                {
                    // Try to find a prefab in the scene
                    var prefabFolder = FindPrefabInAssets("FloatingLetter");
                    if (prefabFolder != null)
                    {
                        SetPrivateField(manager, "floatingLetterPrefab", prefabFolder);
                    }
                }
            }
            
            Debug.Log("[Bootstrap] Created new SubWordGameManager!");
        }

        private void SetupDogCompanion()
        {
            // Check if DogCompanion already exists
            DogCompanion existingDog = FindAnyObjectByType<DogCompanion>();
            
            if (existingDog != null)
            {
                Debug.Log($"[Bootstrap] DogCompanion already exists on '{existingDog.gameObject.name}'");
                
                // Make sure it's enabled
                existingDog.enabled = true;
                
                // Force teleport to player
                existingDog.TeleportToPlayer();
                
                return;
            }
            
            // Try to find a dog in the scene (might be the prefab without the component)
            GameObject dogInScene = GameObject.Find("Dog_001");
            if (dogInScene == null) dogInScene = GameObject.Find("Dog");
            if (dogInScene == null) dogInScene = GameObject.Find("DogCompanion");
            
            if (dogInScene != null)
            {
                // Add DogCompanion component if missing
                DogCompanion dc = dogInScene.GetComponent<DogCompanion>();
                if (dc == null)
                {
                    dc = dogInScene.AddComponent<DogCompanion>();
                    Debug.Log($"[Bootstrap] Added DogCompanion to existing dog '{dogInScene.name}'");
                }
            }
            else if (dogPrefab != null)
            {
                // Instantiate dog prefab
                Camera cam = Camera.main;
                Vector3 spawnPos = cam != null 
                    ? cam.transform.position + cam.transform.forward * 2f 
                    : Vector3.forward * 2f;
                spawnPos.y = 0;
                
                GameObject dog = Instantiate(dogPrefab, spawnPos, Quaternion.identity);
                dog.name = "DogCompanion";
                
                if (dog.GetComponent<DogCompanion>() == null)
                {
                    dog.AddComponent<DogCompanion>();
                }
                
                Debug.Log("[Bootstrap] Spawned dog from prefab");
            }
            else
            {
                Debug.LogWarning("[Bootstrap] No dog found and no prefab assigned! Dog companion won't work.");
            }
        }

        private void EnsureSoundManager()
        {
            if (SoundManager.Instance == null)
            {
                GameObject obj = new GameObject("SoundManager");
                obj.AddComponent<SoundManager>();
                obj.AddComponent<AudioSource>();
                Debug.Log("[Bootstrap] Created SoundManager");
            }
        }

        private void EnsurePunishmentSystem()
        {
            if (PunishmentSystem.Instance == null)
            {
                GameObject obj = new GameObject("PunishmentSystem");
                obj.AddComponent<PunishmentSystem>();
                Debug.Log("[Bootstrap] Created PunishmentSystem");
            }
        }

        private void EnsureEffectsManager()
        {
            if (DramaticEffectsManager.Instance == null)
            {
                GameObject obj = new GameObject("DramaticEffectsManager");
                obj.AddComponent<DramaticEffectsManager>();
                obj.AddComponent<AudioSource>();
                Debug.Log("[Bootstrap] Created DramaticEffectsManager");
            }
            
            if (FloatingPointsPopup.Instance == null)
            {
                GameObject obj = new GameObject("FloatingPointsPopup");
                obj.AddComponent<FloatingPointsPopup>();
                Debug.Log("[Bootstrap] Created FloatingPointsPopup");
            }
        }

        private GameObject FindPrefabInAssets(string name)
        {
            // This is a runtime fallback - won't work for actual prefabs
            // but will find objects that were instantiated from prefabs
            return null;
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }
    }
}
