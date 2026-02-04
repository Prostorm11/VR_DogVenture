using UnityEngine;
using UnityEngine.AI;

namespace VRDogVenture.Dog
{
    /// <summary>
    /// Controls dog companion behavior - following, reactions, and animations.
    /// Works with or without NavMeshAgent.
    /// </summary>
    public class DogCompanion : MonoBehaviour
    {
        [Header("Follow Settings")]
        [SerializeField] private Transform target; // Usually the player/camera
        [SerializeField] private float followDistance = 2f; // Distance at which dog starts following
        [SerializeField] private float stopDistance = 1.5f;
        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private float runSpeed = 5f;
        [SerializeField] private float runThreshold = 5f;
        
        /// <summary>
        /// Get the follow distance setting
        /// </summary>
        public float FollowDistance => followDistance;
        
        [Header("Position Offset")]
        [SerializeField] private float sideOffset = 1f;
        [SerializeField] private float forwardOffset = 2f;
        [SerializeField] private bool stayInFront = true;
        
        [Header("Animation")]
        [SerializeField] private string speedParam = "Vert";
        [SerializeField] private string stateParam = "State";
        
        [Header("Reactions")]
        [SerializeField] private bool reactToCorrectAnswers = true;
        [SerializeField] private bool reactToWrongAnswers = true;
        
        // Components
        private Animator animator;
        private NavMeshAgent navAgent;
        private DogBarkController barkController;
        
        // State
        private bool isFollowing = true;
        private bool isMoving = false;
        private Vector3 targetPosition;
        
        private void Start()
        {
            // Get components
            animator = GetComponentInChildren<Animator>();
            navAgent = GetComponent<NavMeshAgent>();
            barkController = GetComponent<DogBarkController>();
            
            if (barkController == null)
            {
                barkController = gameObject.AddComponent<DogBarkController>();
            }
            
            // Find player if no target assigned
            if (target == null)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    target = cam.transform;
                }
            }
            
            // Configure NavMeshAgent if present
            if (navAgent != null)
            {
                navAgent.speed = walkSpeed;
                navAgent.stoppingDistance = stopDistance;
            }
            
            Debug.Log($"[DogCompanion] Ready. Target: {(target != null ? target.name : "None")}");
        }
        
        private void Update()
        {
            if (!isFollowing || target == null) return;
            
            UpdateFollowing();
            UpdateAnimation();
        }
        
        private void UpdateFollowing()
        {
            // Calculate target position
            Vector3 sideDir = stayInFront ? -target.right : target.right;
            Vector3 forwardDir = target.forward;
            forwardDir.y = 0;
            forwardDir.Normalize();
            sideDir.y = 0;
            sideDir.Normalize();
            
            targetPosition = target.position + (forwardDir * forwardOffset) + (sideDir * sideOffset);
            targetPosition.y = transform.position.y; // Keep on ground
            
            float distance = Vector3.Distance(transform.position, targetPosition);
            
            if (distance > stopDistance)
            {
                isMoving = true;
                bool shouldRun = distance > runThreshold;
                float speed = shouldRun ? runSpeed : walkSpeed;
                
                if (navAgent != null && navAgent.enabled)
                {
                    // Use NavMeshAgent
                    navAgent.speed = speed;
                    navAgent.SetDestination(targetPosition);
                }
                else
                {
                    // Manual movement
                    Vector3 direction = (targetPosition - transform.position).normalized;
                    transform.position += direction * speed * Time.deltaTime;
                    
                    // Rotate to face movement direction
                    if (direction.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(direction);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 5f * Time.deltaTime);
                    }
                }
            }
            else
            {
                isMoving = false;
                
                if (navAgent != null && navAgent.enabled)
                {
                    navAgent.ResetPath();
                }
                
                // Face the player when idle
                Vector3 lookDir = target.position - transform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 3f * Time.deltaTime);
                }
            }
        }
        
        private void UpdateAnimation()
        {
            // Check if animator exists and has a valid runtime controller
            if (animator == null || animator.runtimeAnimatorController == null) return;
            
            float distance = Vector3.Distance(transform.position, targetPosition);
            bool shouldRun = distance > runThreshold;
            
            if (isMoving)
            {
                float animSpeed = shouldRun ? 1f : 0.5f;
                animator.SetFloat(speedParam, animSpeed * 2f);
                animator.SetFloat(stateParam, shouldRun ? 1f : 0.3f);
            }
            else
            {
                animator.SetFloat(speedParam, 0f);
                animator.SetFloat(stateParam, 0f);
            }
        }
        
        #region Public Methods
        
        /// <summary>
        /// Start following the target
        /// </summary>
        public void StartFollowing()
        {
            isFollowing = true;
            Debug.Log("[DogCompanion] Started following");
        }
        
        /// <summary>
        /// Stop following
        /// </summary>
        public void StopFollowing()
        {
            isFollowing = false;
            isMoving = false;
            
            if (navAgent != null)
            {
                navAgent.ResetPath();
            }
            
            UpdateAnimation();
            Debug.Log("[DogCompanion] Stopped following");
        }
        
        /// <summary>
        /// Set a new follow target
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
        
        /// <summary>
        /// React to correct answer - happy bark!
        /// </summary>
        public void OnCorrectAnswer()
        {
            if (!reactToCorrectAnswers) return;
            
            if (barkController != null)
            {
                barkController.BarkHappy();
            }
            
            Debug.Log("[DogCompanion] Reacted to correct answer");
        }
        
        /// <summary>
        /// React to wrong answer - alert bark
        /// </summary>
        public void OnWrongAnswer()
        {
            if (!reactToWrongAnswers) return;
            
            if (barkController != null)
            {
                barkController.BarkAlert();
            }
            
            Debug.Log("[DogCompanion] Reacted to wrong answer");
        }
        
        /// <summary>
        /// Bark to dismiss bees
        /// </summary>
        public void DismissBees()
        {
            if (barkController != null)
            {
                barkController.BarkDismiss();
            }
            
            // Stop punishment if active
            if (VRDogVenture.Punishment.PunishmentSystem.Instance != null)
            {
                VRDogVenture.Punishment.PunishmentSystem.Instance.StopPunishment();
            }
            
            Debug.Log("[DogCompanion] Dismissed bees!");
        }
        
        /// <summary>
        /// Simple bark
        /// </summary>
        public void Bark()
        {
            if (barkController != null)
            {
                barkController.Bark();
            }
        }
        
        #endregion
        
        #region Properties
        
        public bool IsFollowing => isFollowing;
        public bool IsMoving => isMoving;
        public Transform Target => target;
        
        #endregion
    }
}
