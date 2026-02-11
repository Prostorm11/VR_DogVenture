using System.Collections;
using UnityEngine;
using VRProject.Events;
using VRProject.Core;

namespace VRProject.Punishment
{
    /// <summary>
    /// Manages punishment effects when player makes mistakes.
    /// Includes bee stings, screen effects, and triggers dog reactions.
    /// </summary>
    public class PunishmentSystem : MonoBehaviour
    {
        public static PunishmentSystem Instance { get; private set; }

        [Header("Punishment Settings")]
        [SerializeField] private int mistakesBeforePunishment = 1;
        [SerializeField] private int pointsLostPerMistake = 10;
        [SerializeField] private float punishmentCooldown = 3f;

        [Header("Bee Swarm")]
        [SerializeField] private GameObject beeSwarmPrefab;
        [SerializeField] private int beesPerSwarm = 15; // INCREASED for more impact!
        [SerializeField] private float beeAttackDuration = 3f; // Longer attack duration
        [SerializeField] private Transform playerHead; // XR Camera

        [Header("Visual Effects")]
        [SerializeField] private GameObject stingVFXPrefab;
        [SerializeField] private Material screenFlashMaterial;
        [SerializeField] private Color punishmentFlashColor = new Color(1f, 0f, 0f, 0.3f);

        [Header("Camera Shake (Optional)")]
        [SerializeField] private bool enableCameraShake = true;
        [SerializeField] private float shakeIntensity = 0.1f;
        [SerializeField] private float shakeDuration = 0.3f;

        [Header("Haptic Feedback")]
        [SerializeField] private bool enableHaptics = true;
        [SerializeField] private float hapticIntensity = 0.5f;
        [SerializeField] private float hapticDuration = 0.2f;

        // State
        private int consecutiveMistakes = 0;
        private bool canPunish = true;
        private BeeSwarm activeBeeSwarm;
        private bool isContinuousPunishmentActive = false;
        private Coroutine continuousPunishmentCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnWordIncorrect += OnMistake;
            GameEvents.OnWordCorrect += OnCorrect;
        }

        private void OnDisable()
        {
            GameEvents.OnWordIncorrect -= OnMistake;
            GameEvents.OnWordCorrect -= OnCorrect;
        }

        private void Start()
        {
            // Find player head if not assigned
            if (playerHead == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    playerHead = mainCam.transform;
                }
            }
        }

        private void OnMistake(string attemptedWord)
        {
            consecutiveMistakes++;
            Debug.Log($"[Punishment] Mistake #{consecutiveMistakes}: '{attemptedWord}'");

            // Always show minor feedback
            ShowMinorPunishment();

            // Major punishment after threshold
            if (consecutiveMistakes >= mistakesBeforePunishment && canPunish)
            {
                StartCoroutine(ExecuteMajorPunishment());
            }

            // Trigger dog sad reaction
            GameEvents.TriggerDogReaction(DogReactionType.Sad);
        }

        private void OnCorrect(string word, int points)
        {
            // Reset mistake counter on success
            consecutiveMistakes = 0;
        }

        /// <summary>
        /// Minor feedback for every mistake.
        /// </summary>
        private void ShowMinorPunishment()
        {
            // Play sound
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayWordIncorrect();
            }

            // Quick haptic buzz
            if (enableHaptics)
            {
                TriggerHaptics(0.3f, 0.1f);
            }

            // Deduct points from GameManager
            if (GameManager.Instance != null && pointsLostPerMistake > 0)
            {
                GameManager.Instance.DeductPoints(pointsLostPerMistake);
            }
        }

        /// <summary>
        /// Major punishment sequence with bees!
        /// </summary>
        private IEnumerator ExecuteMajorPunishment()
        {
            canPunish = false;
            Debug.Log("[Punishment] Executing bee swarm punishment!");

            // Play warning sound
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayBeeSwarm();
            }

            // Spawn bee swarm
            yield return StartCoroutine(BeeSwarmAttack());

            // Screen flash
            StartCoroutine(ScreenFlash());

            // Camera shake
            if (enableCameraShake)
            {
                StartCoroutine(CameraShake());
            }

            // Strong haptics
            if (enableHaptics)
            {
                TriggerHaptics(hapticIntensity, hapticDuration);
            }

            // Play ouch sound
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayOuch();
                SoundManager.Instance.PlayBeeSting();
            }

            // Trigger dog growl/angry reaction
            GameEvents.TriggerDogReaction(DogReactionType.Angry);

            // Reset
            consecutiveMistakes = 0;
            
            // Cooldown before next punishment
            yield return new WaitForSeconds(punishmentCooldown);
            canPunish = true;
        }

        /// <summary>
        /// Bee swarm attack sequence.
        /// </summary>
        private IEnumerator BeeSwarmAttack()
        {
            if (playerHead == null)
            {
                Debug.LogWarning("[Punishment] No player head found for bee attack!");
                yield break;
            }

            // Spawn swarm behind/above player
            Vector3 spawnPos = playerHead.position + playerHead.forward * -1f + Vector3.up * 0.5f;
            GameObject swarmObj;
            
            if (beeSwarmPrefab != null)
            {
                swarmObj = Instantiate(beeSwarmPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                // Create bee swarm at runtime if no prefab
                swarmObj = new GameObject("BeeSwarm_Runtime");
                swarmObj.transform.position = spawnPos;
                swarmObj.AddComponent<AudioSource>();
            }
            
            activeBeeSwarm = swarmObj.GetComponent<BeeSwarm>();
            if (activeBeeSwarm == null)
            {
                activeBeeSwarm = swarmObj.AddComponent<BeeSwarm>();
            }

            activeBeeSwarm.SetBeeCount(beesPerSwarm);
            activeBeeSwarm.AttackTarget(playerHead, beeAttackDuration);
            
            Debug.Log($"[Punishment] Spawned bee swarm with {beesPerSwarm} bees!");

            yield return new WaitForSeconds(beeAttackDuration);

            // Spawn sting VFX
            if (stingVFXPrefab != null)
            {
                Vector3 stingPos = playerHead.position + playerHead.forward * 0.3f;
                GameObject sting = Instantiate(stingVFXPrefab, stingPos, Quaternion.identity);
                Destroy(sting, 2f);
            }

            yield return new WaitForSeconds(1f);

            if (swarmObj != null)
            {
                Destroy(swarmObj);
            }
        }

        /// <summary>
        /// Red screen flash effect.
        /// </summary>
        private IEnumerator ScreenFlash()
        {
            // This would typically use a UI overlay or post-processing
            // For VR, you might use a sphere around the camera
            
            // Simple approach: Find/create a flash overlay
            GameObject flashObj = GameObject.Find("PunishmentFlash");
            if (flashObj == null)
            {
                flashObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flashObj.name = "PunishmentFlash";
                flashObj.transform.SetParent(playerHead);
                flashObj.transform.localPosition = Vector3.zero;
                flashObj.transform.localScale = Vector3.one * 0.5f;
                
                // Invert normals for inside-out sphere (simple approach)
                Renderer rend = flashObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    mat.color = punishmentFlashColor;
                    mat.SetFloat("_Surface", 1); // Transparent
                    mat.renderQueue = 3000;
                    rend.material = mat;
                }

                // Remove collider
                Collider col = flashObj.GetComponent<Collider>();
                if (col != null) Destroy(col);
                
                flashObj.SetActive(false);
            }

            // Flash sequence
            flashObj.SetActive(true);
            Renderer renderer = flashObj.GetComponent<Renderer>();
            
            float elapsed = 0f;
            float duration = 0.3f;
            
            while (elapsed < duration)
            {
                float alpha = Mathf.PingPong(elapsed * 6f, 0.4f);
                if (renderer != null)
                {
                    Color c = punishmentFlashColor;
                    c.a = alpha;
                    renderer.material.color = c;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            flashObj.SetActive(false);
        }

        /// <summary>
        /// Camera shake effect.
        /// </summary>
        private IEnumerator CameraShake()
        {
            if (playerHead == null) yield break;

            Vector3 originalPos = playerHead.localPosition;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                float x = Random.Range(-1f, 1f) * shakeIntensity;
                float y = Random.Range(-1f, 1f) * shakeIntensity;

                playerHead.localPosition = originalPos + new Vector3(x, y, 0);
                
                elapsed += Time.deltaTime;
                yield return null;
            }

            playerHead.localPosition = originalPos;
        }

        /// <summary>
        /// Trigger controller haptics.
        /// </summary>
        private void TriggerHaptics(float intensity, float duration)
        {
            // This requires XR Interaction Toolkit haptics
            // Implementation depends on your XR setup
            
            #if UNITY_XR_INTERACTION_TOOLKIT
            // Example for XR Interaction Toolkit:
            // var leftController = // get left controller
            // var rightController = // get right controller
            // leftController?.SendHapticImpulse(intensity, duration);
            // rightController?.SendHapticImpulse(intensity, duration);
            #endif

            Debug.Log($"[Haptics] Intensity: {intensity}, Duration: {duration}");
        }

        /// <summary>
        /// Manually trigger punishment (for testing).
        /// </summary>
        public void TriggerPunishment()
        {
            if (canPunish)
            {
                StartCoroutine(ExecuteMajorPunishment());
            }
        }
        
        /// <summary>
        /// Start continuous bee stinging until stopped.
        /// Bees will keep stinging until StopContinuousPunishment() is called.
        /// </summary>
        public void StartContinuousPunishment()
        {
            if (isContinuousPunishmentActive) return; // Already active
            
            Debug.Log("[Punishment] Starting CONTINUOUS bee punishment!");
            isContinuousPunishmentActive = true;
            continuousPunishmentCoroutine = StartCoroutine(ContinuousBeeAttack());
            
            // Trigger dog angry reaction
            GameEvents.TriggerDogReaction(DogReactionType.Angry);
        }
        
        /// <summary>
        /// Stop continuous punishment - called when player gets word correct.
        /// </summary>
        public void StopContinuousPunishment()
        {
            if (!isContinuousPunishmentActive) return;
            
            Debug.Log("[Punishment] Stopping continuous bee punishment!");
            isContinuousPunishmentActive = false;
            
            if (continuousPunishmentCoroutine != null)
            {
                StopCoroutine(continuousPunishmentCoroutine);
                continuousPunishmentCoroutine = null;
            }
            
            // Dismiss active bee swarm
            DismissBees();
        }
        
        /// <summary>
        /// Dismiss the active bee swarm (called by dog bark).
        /// </summary>
        public void DismissBees()
        {
            if (activeBeeSwarm != null)
            {
                Debug.Log("[Punishment] Dismissing bee swarm!");
                activeBeeSwarm.Dismiss();
            }
            
            // Also find any other bee swarms and dismiss them
            BeeSwarm[] allSwarms = FindObjectsByType<BeeSwarm>(FindObjectsSortMode.None);
            foreach (var swarm in allSwarms)
            {
                swarm.Dismiss();
            }
        }
        
        /// <summary>
        /// Continuous bee attack until stopped.
        /// </summary>
        private IEnumerator ContinuousBeeAttack()
        {
            if (playerHead == null)
            {
                Debug.LogWarning("[Punishment] No player head found for continuous bee attack!");
                yield break;
            }
            
            // Spawn initial swarm
            SpawnBeeSwarm();
            
            // Keep attacking until stopped
            while (isContinuousPunishmentActive)
            {
                // Periodic sting effects
                yield return new WaitForSeconds(1.5f);
                
                if (!isContinuousPunishmentActive) break;
                
                // Sting effect
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayBeeSting();
                }
                
                // Quick haptic
                if (enableHaptics)
                {
                    TriggerHaptics(0.3f, 0.1f);
                }
                
                // Quick screen flash
                StartCoroutine(QuickFlash());
                
                // Spawn sting VFX
                if (stingVFXPrefab != null && playerHead != null)
                {
                    Vector3 stingPos = playerHead.position + Random.onUnitSphere * 0.3f;
                    stingPos.y = playerHead.position.y;
                    GameObject sting = Instantiate(stingVFXPrefab, stingPos, Quaternion.identity);
                    Destroy(sting, 1f);
                }
            }
        }
        
        /// <summary>
        /// Spawn a bee swarm to attack player.
        /// </summary>
        private void SpawnBeeSwarm()
        {
            if (playerHead == null) return;
            
            Vector3 spawnPos = playerHead.position + playerHead.forward * -1f + Vector3.up * 0.5f;
            GameObject swarmObj;
            
            if (beeSwarmPrefab != null)
            {
                swarmObj = Instantiate(beeSwarmPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                swarmObj = new GameObject("BeeSwarm_Runtime");
                swarmObj.transform.position = spawnPos;
                swarmObj.AddComponent<AudioSource>();
            }
            
            activeBeeSwarm = swarmObj.GetComponent<BeeSwarm>();
            if (activeBeeSwarm == null)
            {
                activeBeeSwarm = swarmObj.AddComponent<BeeSwarm>();
            }
            
            activeBeeSwarm.SetBeeCount(beesPerSwarm);
            
            // For continuous attack, use a very long duration (until dismissed)
            activeBeeSwarm.AttackTarget(playerHead, 999f);
            
            // Play warning sound
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayBeeSwarm();
            }
            
            Debug.Log($"[Punishment] Spawned continuous bee swarm with {beesPerSwarm} bees!");
        }
        
        /// <summary>
        /// Quick screen flash for sting.
        /// </summary>
        private IEnumerator QuickFlash()
        {
            GameObject flashObj = GameObject.Find("PunishmentFlash");
            if (flashObj == null) yield break;
            
            flashObj.SetActive(true);
            yield return new WaitForSeconds(0.1f);
            flashObj.SetActive(false);
        }
        
        /// <summary>
        /// Check if punishment is currently active.
        /// </summary>
        public bool IsPunishmentActive => isContinuousPunishmentActive;

        /// <summary>
        /// Stop all punishment effects (called by DogCompanion when barking).
        /// </summary>
        public void StopPunishment()
        {
            Debug.Log("[Punishment] StopPunishment called!");
            StopContinuousPunishment();
            canPunish = true;
            consecutiveMistakes = 0;
        }
    }
}
