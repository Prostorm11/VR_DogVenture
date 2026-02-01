using UnityEngine;
using VRDogVenture.Core;
using VRDogVenture.Punishment;
using VRDogVenture.Dog;
using VRDogVenture.UI;
using VRDogVenture.WordPuzzle;
using VRDogVenture.Effects;

namespace VRDogVenture.Setup
{
    /// <summary>
    /// Automatically sets up game systems in BasicScene.
    /// Add this to a GameObject in your BasicScene to ensure all systems are created.
    /// </summary>
    public class GameSetup : MonoBehaviour
    {
        [Header("Prefabs (Optional - will create if not assigned)")]
        [SerializeField] private GameObject beePrefab; // Assign FantasyBee.prefab
        [SerializeField] private GameObject dogPrefab; // Assign Dog_001.prefab

        [Header("Settings")]
        [SerializeField] private bool autoSetup = true;

        private void Awake()
        {
            if (autoSetup)
            {
                SetupAllSystems();
            }
        }

        [ContextMenu("Setup All Systems")]
        public void SetupAllSystems()
        {
            Debug.Log("[GameSetup] Setting up all game systems...");

            SetupPunishmentSystem();
            SetupDogCompanion();
            SetupGameMenuUI();
            SetupSoundManager();
            SetupFloatingPointsPopup();
            SetupDramaticEffects();
            SetupSubWordGameManager();

            Debug.Log("[GameSetup] All systems setup complete!");
        }

        private void SetupPunishmentSystem()
        {
            // Check if PunishmentSystem already exists
            if (PunishmentSystem.Instance != null)
            {
                Debug.Log("[GameSetup] PunishmentSystem already exists.");
                return;
            }

            // Create PunishmentSystem
            GameObject psObj = new GameObject("PunishmentSystem");
            PunishmentSystem ps = psObj.AddComponent<PunishmentSystem>();

            // Try to find and assign the bee prefab
            if (beePrefab != null)
            {
                // Assign via reflection or serialized field
                SetPrivateField(ps, "beeSwarmPrefab", CreateBeeSwarmPrefab());
            }
            else
            {
                // Create a runtime bee swarm prefab
                SetPrivateField(ps, "beeSwarmPrefab", CreateBeeSwarmPrefab());
            }

            Debug.Log("[GameSetup] Created PunishmentSystem");
        }

        private GameObject CreateBeeSwarmPrefab()
        {
            // Create a prefab-like object for BeeSwarm
            GameObject swarmObj = new GameObject("BeeSwarm_Runtime");
            BeeSwarm swarm = swarmObj.AddComponent<BeeSwarm>();
            swarmObj.AddComponent<AudioSource>();

            // If we have the FantasyBee prefab, assign it
            if (beePrefab != null)
            {
                SetPrivateField(swarm, "beePrefab", beePrefab);
            }

            // Deactivate so it acts as a prefab
            swarmObj.SetActive(false);
            return swarmObj;
        }

        private void SetupDogCompanion()
        {
            // Check if DogCompanion already exists
            if (DogCompanion.Instance != null)
            {
                Debug.Log("[GameSetup] DogCompanion already exists.");
                return;
            }

            // Also check if a dog with DogARGuideController exists (from prefab in scene)
            var existingDog = FindAnyObjectByType<DogARGuideController>();
            if (existingDog != null)
            {
                // Dog prefab is already in scene - just add DogCompanion to it
                var dogObj = existingDog.gameObject;
                if (dogObj.GetComponent<DogCompanion>() == null)
                {
                    dogObj.AddComponent<DogCompanion>();
                    Debug.Log("[GameSetup] Added DogCompanion to existing dog prefab in scene");
                }
                return;
            }

            if (dogPrefab != null)
            {
                // Instantiate from prefab
                Camera mainCam = Camera.main;
                Vector3 spawnPos = mainCam != null 
                    ? mainCam.transform.position + mainCam.transform.forward * 1.5f 
                    : Vector3.forward * 2f;
                spawnPos.y = 0f;

                GameObject dogObj = Instantiate(dogPrefab, spawnPos, Quaternion.identity);
                dogObj.name = "DogCompanion";

                // Add DogCompanion script if not already present
                if (dogObj.GetComponent<DogCompanion>() == null)
                {
                    dogObj.AddComponent<DogCompanion>();
                }

                Debug.Log("[GameSetup] Spawned Dog from prefab with DogCompanion");
            }
            else
            {
                // Try to load from Resources
                GameObject loadedPrefab = Resources.Load<GameObject>("DogHuman/Dog_001");
                if (loadedPrefab != null)
                {
                    Camera mainCam = Camera.main;
                    Vector3 spawnPos = mainCam != null 
                        ? mainCam.transform.position + mainCam.transform.forward * 1.5f 
                        : Vector3.forward * 2f;
                    spawnPos.y = 0f;

                    GameObject dogObj = Instantiate(loadedPrefab, spawnPos, Quaternion.identity);
                    dogObj.name = "DogCompanion";

                    if (dogObj.GetComponent<DogCompanion>() == null)
                    {
                        dogObj.AddComponent<DogCompanion>();
                    }

                    Debug.Log("[GameSetup] Spawned Dog from Resources with DogCompanion");
                }
                else
                {
                    // Create placeholder dog as last resort
                    Debug.LogWarning("[GameSetup] Dog prefab not assigned and not found in Resources. Creating placeholder. " +
                                     "Please assign Dog_001.prefab to GameSetup's dogPrefab field!");
                    
                    GameObject dogObj = new GameObject("DogCompanion_Placeholder");
                    dogObj.AddComponent<DogCompanion>();
                    
                    // Add a simple visual placeholder
                    GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    visual.transform.SetParent(dogObj.transform);
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localScale = new Vector3(0.3f, 0.2f, 0.5f);
                    Destroy(visual.GetComponent<Collider>());

                    Camera mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        dogObj.transform.position = mainCam.transform.position + mainCam.transform.forward * 1.5f;
                        dogObj.transform.position = new Vector3(dogObj.transform.position.x, 0f, dogObj.transform.position.z);
                    }
                }
            }
        }

        private void SetupGameMenuUI()
        {
            // Check if GameMenuUI already exists
            GameMenuUI existingMenu = FindAnyObjectByType<GameMenuUI>();
            if (existingMenu != null)
            {
                Debug.Log("[GameSetup] GameMenuUI already exists.");
                return;
            }

            // Create GameMenuUI
            GameObject menuObj = new GameObject("GameMenuUI");
            menuObj.AddComponent<GameMenuUI>();
            Debug.Log("[GameSetup] Created GameMenuUI");
        }

        private void SetupSoundManager()
        {
            // Check if SoundManager exists
            if (SoundManager.Instance != null)
            {
                Debug.Log("[GameSetup] SoundManager already exists.");
                return;
            }

            // Create SoundManager
            GameObject soundObj = new GameObject("SoundManager");
            soundObj.AddComponent<SoundManager>();
            soundObj.AddComponent<AudioSource>();
            Debug.Log("[GameSetup] Created SoundManager");
        }

        private void SetupFloatingPointsPopup()
        {
            // Check if FloatingPointsPopup already exists
            if (FloatingPointsPopup.Instance != null)
            {
                Debug.Log("[GameSetup] FloatingPointsPopup already exists.");
                return;
            }

            // Create FloatingPointsPopup
            GameObject popupObj = new GameObject("FloatingPointsPopup");
            popupObj.AddComponent<FloatingPointsPopup>();
            Debug.Log("[GameSetup] Created FloatingPointsPopup");
        }

        private void SetupDramaticEffects()
        {
            // Check if DramaticEffectsManager already exists
            if (DramaticEffectsManager.Instance != null)
            {
                Debug.Log("[GameSetup] DramaticEffectsManager already exists.");
                return;
            }

            // Create DramaticEffectsManager
            GameObject effectsObj = new GameObject("DramaticEffectsManager");
            effectsObj.AddComponent<DramaticEffectsManager>();
            effectsObj.AddComponent<AudioSource>();
            Debug.Log("[GameSetup] Created DramaticEffectsManager");
        }

        private void SetupSubWordGameManager()
        {
            // Check if SubWordGameManager already exists
            if (SubWordGameManager.Instance != null)
            {
                Debug.Log("[GameSetup] SubWordGameManager already exists.");
                return;
            }

            // Create SubWordGameManager
            GameObject gameObj = new GameObject("SubWordGameManager");
            SubWordGameManager manager = gameObj.AddComponent<SubWordGameManager>();
            
            // Try to find FloatingLetter prefab
            GameObject letterPrefab = Resources.Load<GameObject>("Prefabs/FloatingLetter");
            if (letterPrefab != null)
            {
                SetPrivateField(manager, "floatingLetterPrefab", letterPrefab);
            }
            
            Debug.Log("[GameSetup] Created SubWordGameManager");
        }

        // Helper to set private serialized fields
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
