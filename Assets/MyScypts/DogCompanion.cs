using System.Collections;
using UnityEngine;
using VRDogVenture.Events;
using VRDogVenture.Punishment;
using ithappy.Animals_FREE;

namespace VRDogVenture.Dog
{
    /// <summary>
    /// VR Dog companion that follows the player, reacts to game events,
    /// and can dismiss bees when player gets points after being stung.
    /// Uses CreatureMover from ithappy.Animals_FREE for animation.
    /// </summary>
    public class DogCompanion : MonoBehaviour
    {
        public static DogCompanion Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Transform player; // XR Camera
        [SerializeField] private Animator animator;
        [SerializeField] private CreatureMover creatureMover; // From ithappy
        [SerializeField] private DogBarkController barkController; // For bark animations

        [Header("Following Settings")]
        [SerializeField] private float preferredDistance = 1.5f; // Distance in front of player (guide)
        [SerializeField] private float sideOffset = 0.8f; // Offset to the left when stationary
        [SerializeField] private float stopThreshold = 0.3f; // Stops if within this range
        [SerializeField] private float catchUpDistance = 4f; // Distance where dog runs
        [SerializeField] private float aheadDistance = 2.0f; // How far ahead when guiding
        [SerializeField] private float playerMovementThreshold = 0.1f; // Speed to consider player "moving"

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 1.5f;
        [SerializeField] private float runSpeed = 3.5f;
        [SerializeField] private float rotationSpeed = 5f;

        [Header("Animation Parameters (uses CreatureMover's Vert/State by default)")]
        // Note: The Dog.controller from ithappy only has "Vert" and "State" parameters
        // CreatureMover handles these automatically - we don't set them directly
        // These trigger params are optional - only used if your animator has them
        [SerializeField] private string happyTrigger = "Happy";
        [SerializeField] private string sadTrigger = "Sad";
        [SerializeField] private string barkTrigger = "Bark";
        [SerializeField] private string alertTrigger = "Alert";
        [SerializeField] private bool useAnimatorTriggers = false; // Set true only if animator has these triggers

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip happyBark;
        [SerializeField] private AudioClip sadWhimper;
        [SerializeField] private AudioClip alertBark;
        [SerializeField] private AudioClip cheerBark; // Bark that dismisses bees

        [Header("Bee Dismissal")]
        [SerializeField] private float barkRadius = 5f; // Radius where bark affects bees
        [SerializeField] private ParticleSystem barkWaveVFX; // Visual wave when barking

        [Header("Debug / State")]
        [SerializeField] private bool isFollowing = true; // Enable/disable following
        [SerializeField] private bool showDebugInfo = true; // Show debug logs
        
        // Runtime state (shown in Inspector for debugging)
        [Space(10)]
        [Header("--- Runtime State (Read Only) ---")]
        [SerializeField] private bool _playerFound = false;
        [SerializeField] private bool _isMovingToTarget = false;
        [SerializeField] private float _distanceToTarget = 0f;
        
        // Internal state
        private bool hasBeenStung = false; // Tracks if player recently got stung
        private float currentSpeed = 0f;
        private Vector3 targetPosition;
        private Vector3 lastPlayerPosition;
        private Vector3 playerVelocity;
        private bool playerIsMoving = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Find player if not assigned - TRY MULTIPLE METHODS
            if (player == null)
            {
                // Method 1: Main camera
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    player = mainCam.transform;
                    Debug.Log($"[DogCompanion] Found player via Camera.main: {player.name}");
                }
            }
            
            if (player == null)
            {
                // Method 2: Find XR Origin
                var xrOrigin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (xrOrigin != null)
                {
                    player = xrOrigin.Camera?.transform;
                    Debug.Log($"[DogCompanion] Found player via XROrigin: {player?.name}");
                }
            }

            if (player == null)
            {
                // Method 3: Find any camera
                Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                foreach (var cam in cameras)
                {
                    if (cam.gameObject.activeInHierarchy)
                    {
                        player = cam.transform;
                        Debug.Log($"[DogCompanion] Found player via Camera search: {player.name}");
                        break;
                    }
                }
            }

            // Get CreatureMover if not assigned (for animations)
            if (creatureMover == null)
                creatureMover = GetComponent<CreatureMover>();

            // Get animator - search children too
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            
            // Get or add bark controller
            if (barkController == null)
            {
                barkController = GetComponent<DogBarkController>();
                if (barkController == null)
                {
                    barkController = gameObject.AddComponent<DogBarkController>();
                }
            }

            // Disable any DogARGuideController to prevent conflicts
            var arGuide = GetComponent<DogARGuideController>();
            if (arGuide != null)
            {
                arGuide.enabled = false;
                Debug.Log("[DogCompanion] Disabled DogARGuideController to prevent movement conflicts");
            }

            // Update debug state
            _playerFound = (player != null);

            // Log status
            Debug.Log($"[DogCompanion] ========== INITIALIZED ==========");
            Debug.Log($"[DogCompanion] Player: {(player != null ? player.name : "NOT FOUND!")}");
            Debug.Log($"[DogCompanion] Position: {transform.position}");
            Debug.Log($"[DogCompanion] isFollowing: {isFollowing}");
            Debug.Log($"[DogCompanion] CreatureMover: {(creatureMover != null ? "Found" : "NOT FOUND")}");
            Debug.Log($"[DogCompanion] Animator: {(animator != null ? "Found" : "NOT FOUND")}");
            Debug.Log($"[DogCompanion] ================================");
            
            if (player == null)
            {
                Debug.LogError("[DogCompanion] CRITICAL: No player/camera found! Dog cannot follow!");
            }
        }

        private void OnEnable()
        {
            GameEvents.OnDogReaction += HandleDogReaction;
            GameEvents.OnWordCorrect += OnPlayerCorrect;
            GameEvents.OnWordIncorrect += OnPlayerMistake;
            GameEvents.OnScoreChanged += OnScoreChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnDogReaction -= HandleDogReaction;
            GameEvents.OnWordCorrect -= OnPlayerCorrect;
            GameEvents.OnWordIncorrect -= OnPlayerMistake;
            GameEvents.OnScoreChanged -= OnScoreChanged;
        }

        private void Update()
        {
            if (!isFollowing || player == null) 
            {
                // Debug why we're not following
                if (Time.frameCount % 300 == 0) // Log every ~5 seconds
                {
                    Debug.Log($"[DogCompanion] NOT FOLLOWING - isFollowing: {isFollowing}, player: {(player != null ? player.name : "NULL")}");
                    
                    // Try to find player again if null
                    if (player == null)
                    {
                        Camera mainCam = Camera.main;
                        if (mainCam != null)
                        {
                            player = mainCam.transform;
                            Debug.Log($"[DogCompanion] RE-FOUND player: {player.name}");
                        }
                    }
                }
                return;
            }

            UpdateFollowBehavior();
            UpdateAnimations();
        }

        private void UpdateFollowBehavior()
        {
            // Track player movement
            if (lastPlayerPosition != Vector3.zero)
            {
                playerVelocity = (player.position - lastPlayerPosition) / Time.deltaTime;
                playerVelocity.y = 0; // Only care about horizontal movement
                playerIsMoving = playerVelocity.magnitude > playerMovementThreshold;
            }
            lastPlayerPosition = player.position;
            
            // GUIDE BEHAVIOR: Position depends on whether player is moving
            if (playerIsMoving)
            {
                // AHEAD OF PLAYER when moving - dog guides/leads
                // Position in the direction player is moving
                Vector3 moveDirection = playerVelocity.normalized;
                if (moveDirection.sqrMagnitude < 0.001f)
                {
                    moveDirection = player.forward;
                }
                targetPosition = player.position + moveDirection * aheadDistance;
            }
            else
            {
                // TO THE LEFT when stationary - dog waits beside player
                Vector3 leftOffset = -player.right * sideOffset;
                Vector3 forwardOffset = player.forward * 0.3f; // Slightly in front to be visible
                targetPosition = player.position + leftOffset + forwardOffset;
            }
            
            targetPosition.y = transform.position.y; // Keep on ground

            float distance = Vector3.Distance(transform.position, targetPosition);
            
            // Update debug state for Inspector
            _distanceToTarget = distance;
            _isMovingToTarget = distance > stopThreshold;

            // Decide movement
            bool shouldRun = distance > catchUpDistance;
            bool shouldMove = distance > stopThreshold;
            
            // Debug logging
            if (showDebugInfo && Time.frameCount % 120 == 0) // Every ~2 seconds
            {
                Debug.Log($"[DogCompanion] Distance: {distance:F2}, Moving: {shouldMove}, Running: {shouldRun}, PlayerMoving: {playerIsMoving}");
            }

            // Calculate direction to target
            Vector3 direction = (targetPosition - transform.position).normalized;

            // ALWAYS use direct movement for reliability - don't depend on CreatureMover
            if (shouldMove)
            {
                float speed = shouldRun ? runSpeed : walkSpeed;
                
                // Move towards target
                Vector3 movement = direction * speed * Time.deltaTime;
                transform.position += movement;
                
                // Rotate to face movement direction (when moving) or look at player (when stationary)
                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot;
                    if (playerIsMoving)
                    {
                        // Face movement direction when guiding
                        targetRot = Quaternion.LookRotation(direction);
                    }
                    else
                    {
                        // Face toward player when waiting beside
                        Vector3 lookAtPlayer = (player.position - transform.position).normalized;
                        lookAtPlayer.y = 0;
                        if (lookAtPlayer.sqrMagnitude > 0.001f)
                        {
                            targetRot = Quaternion.LookRotation(lookAtPlayer);
                        }
                        else
                        {
                            targetRot = transform.rotation;
                        }
                    }
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                }
            }
            else
            {
                // When stopped, face the player
                Vector3 lookAtPlayer = (player.position - transform.position).normalized;
                lookAtPlayer.y = 0;
                if (lookAtPlayer.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookAtPlayer);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                }
            }

            // Also update CreatureMover for animations (if available)
            if (creatureMover != null)
            {
                Vector2 moveAxis = shouldMove ? new Vector2(0f, 1f) : Vector2.zero;
                creatureMover.SetCommand(moveAxis, targetPosition, shouldRun, shouldMove);
            }

            // Update current speed for animations
            float targetSpeed = shouldMove ? (shouldRun ? runSpeed : walkSpeed) : 0f;
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);
        }

        private void RotateTowards(Vector3 targetPos)
        {
            Vector3 lookDir = targetPos - transform.position;
            lookDir.y = 0f;

            if (lookDir.sqrMagnitude < 0.001f) return;

            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        private void UpdateAnimations()
        {
            // CreatureMover handles walk/run animations via "Vert" and "State" parameters
            // We don't need to set anything manually - movement is animation-driven
            // This method exists for future expansion if needed
        }

        #region Dog Reactions

        private void HandleDogReaction(DogReactionType reaction)
        {
            switch (reaction)
            {
                case DogReactionType.Happy:
                    PlayHappyReaction();
                    break;
                case DogReactionType.Sad:
                    PlaySadReaction();
                    break;
                case DogReactionType.Angry:
                    PlayAlertReaction();
                    break;
                case DogReactionType.Excited:
                    PlayExcitedReaction();
                    break;
            }
        }

        private void PlayHappyReaction()
        {
            Debug.Log("[Dog] Happy reaction - tail wag, happy bark!");

            TrySetTrigger(happyTrigger);
            PlaySound(happyBark);
        }

        private void PlaySadReaction()
        {
            Debug.Log("[Dog] Sad reaction - whimper");

            TrySetTrigger(sadTrigger);
            PlaySound(sadWhimper);
        }

        private void PlayAlertReaction()
        {
            Debug.Log("[Dog] Alert reaction - watching bees");

            TrySetTrigger(alertTrigger);
            PlaySound(alertBark);
        }

        private void PlayExcitedReaction()
        {
            Debug.Log("[Dog] Excited - jumping for joy!");

            TrySetTrigger(happyTrigger);
            TrySetTrigger(barkTrigger);
            PlaySound(happyBark);
        }

        /// <summary>
        /// Safely try to set an animator trigger - won't fail if parameter doesn't exist
        /// </summary>
        private void TrySetTrigger(string triggerName)
        {
            if (!useAnimatorTriggers || animator == null || string.IsNullOrEmpty(triggerName)) 
                return;

            try
            {
                animator.SetTrigger(triggerName);
            }
            catch (System.Exception)
            {
                // Parameter doesn't exist - that's OK, the Dog.controller from ithappy
                // only has Vert/State parameters. Triggers are optional.
            }
        }

        #endregion

        #region Bee Dismissal

        private void OnPlayerMistake(string word)
        {
            hasBeenStung = true;
            PlaySadReaction();
        }

        private void OnPlayerCorrect(string word, int points)
        {
            // If player was previously stung and now got points, dismiss the bees!
            if (hasBeenStung)
            {
                StartCoroutine(DismissBeesWithBark());
                hasBeenStung = false;
            }
            else
            {
                // Just happy reaction
                PlayHappyReaction();
            }
        }

        private void OnScoreChanged(int score)
        {
            // Additional reactions based on milestones
            if (score > 0 && score % 100 == 0)
            {
                PlayExcitedReaction();
            }
        }

        /// <summary>
        /// Dog barks powerfully to dismiss all nearby bees!
        /// </summary>
        private IEnumerator DismissBeesWithBark()
        {
            Debug.Log("[Dog] BARK! Dismissing bees!");

            // Use bark controller for animation
            if (barkController != null)
            {
                barkController.BarkDismiss();
            }
            else
            {
                // Fallback to old method
                TrySetTrigger(barkTrigger);
                PlaySound(cheerBark ?? happyBark);
            }

            // Show bark wave VFX (particle system)
            if (barkWaveVFX != null)
            {
                barkWaveVFX.Play();
            }

            yield return new WaitForSeconds(0.3f);

            // Find and dismiss all bees in radius
            DismissNearbyBees();

            // Trigger event for other systems
            GameEvents.TriggerDogReaction(DogReactionType.Happy);
        }

        private void DismissNearbyBees()
        {
            // Find all BeeSwarm objects
            var beeSwarms = FindObjectsByType<VRDogVenture.Punishment.BeeSwarm>(FindObjectsSortMode.None);
            
            foreach (var swarm in beeSwarms)
            {
                float distance = Vector3.Distance(transform.position, swarm.transform.position);
                if (distance <= barkRadius)
                {
                    swarm.Dismiss();
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set whether the dog should follow the player.
        /// </summary>
        public void SetFollowing(bool follow)
        {
            isFollowing = follow;
            Debug.Log($"[DogCompanion] SetFollowing: {follow}");
            
            if (!follow)
            {
                // Stop movement
                if (creatureMover != null)
                {
                    creatureMover.SetCommand(Vector2.zero, transform.position, false, false);
                }
                currentSpeed = 0f;
            }
        }

        /// <summary>
        /// Teleport dog to a position near the player.
        /// </summary>
        [ContextMenu("Teleport To Player")]
        public void TeleportToPlayer()
        {
            // Find player if null
            if (player == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    player = mainCam.transform;
                }
            }
            
            if (player == null)
            {
                Debug.LogError("[DogCompanion] Cannot teleport - no player found!");
                return;
            }

            Vector3 pos = player.position + player.forward * preferredDistance - player.right * sideOffset;
            pos.y = 0; // Ground level
            transform.position = pos;
            
            Debug.Log($"[DogCompanion] Teleported to {pos}");
        }
        
        /// <summary>
        /// Force start following (for testing).
        /// </summary>
        [ContextMenu("Force Start Following")]
        public void ForceStartFollowing()
        {
            // Find player
            if (player == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    player = mainCam.transform;
                    _playerFound = true;
                    Debug.Log($"[DogCompanion] Found player: {player.name}");
                }
            }
            
            isFollowing = true;
            Debug.Log("[DogCompanion] FORCE START FOLLOWING - isFollowing is now TRUE");
        }
        
        /// <summary>
        /// Debug current state.
        /// </summary>
        [ContextMenu("Debug State")]
        public void DebugState()
        {
            Debug.Log("========== DOG COMPANION STATE ==========");
            Debug.Log($"Player: {(player != null ? player.name : "NULL")}");
            Debug.Log($"isFollowing: {isFollowing}");
            Debug.Log($"Position: {transform.position}");
            Debug.Log($"Target Position: {targetPosition}");
            Debug.Log($"Distance to Target: {_distanceToTarget:F2}");
            Debug.Log($"Player Is Moving: {playerIsMoving}");
            Debug.Log($"CreatureMover: {(creatureMover != null ? "Found" : "NULL")}");
            Debug.Log($"Animator: {(animator != null ? "Found" : "NULL")}");
            Debug.Log("==========================================");
        }

        /// <summary>
        /// Make the dog bark on command.
        /// </summary>
        [ContextMenu("Bark")]
        public void Bark()
        {
            if (barkController != null)
            {
                barkController.Bark();
            }
            else
            {
                TrySetTrigger(barkTrigger);
                PlaySound(happyBark);
            }
        }
        
        /// <summary>
        /// Make the dog bark happily.
        /// </summary>
        public void BarkHappy()
        {
            if (barkController != null)
            {
                barkController.BarkHappy();
            }
            else
            {
                Bark();
            }
        }
        
        /// <summary>
        /// Make the dog bark to dismiss bees (called when player gets word correct during punishment).
        /// </summary>
        public void BarkToDismissBees()
        {
            StartCoroutine(DismissBeesWithBark());
            
            // Also notify punishment system to stop continuous stinging
            if (PunishmentSystem.Instance != null)
            {
                PunishmentSystem.Instance.StopContinuousPunishment();
            }
        }

        #endregion

        private void PlaySound(AudioClip clip)
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                    audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Show bark radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, barkRadius);

            // Show target position
            if (Application.isPlaying && player != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(targetPosition, 0.1f);
            }
        }
    }
}
