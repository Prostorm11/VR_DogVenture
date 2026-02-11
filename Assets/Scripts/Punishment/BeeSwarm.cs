using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRProject.Punishment
{
    /// <summary>
    /// Controls a swarm of bees that attack the player as punishment.
    /// Creates particle-like bee effects that buzz around the target.
    /// </summary>
    public class BeeSwarm : MonoBehaviour
    {
        [Header("Swarm Settings")]
        [SerializeField] private int beeCount = 15;
        [SerializeField] private float swarmRadius = 0.5f;
        [SerializeField] private float buzzSpeed = 5f;
        [SerializeField] private float orbitSpeed = 2f;

        [Header("Attack Settings")]
        #pragma warning disable 0414
        [SerializeField] private float attackRange = 0.3f;
        #pragma warning restore 0414
        [SerializeField] private float stingInterval = 0.5f;

        [Header("Audio")]
        [SerializeField] private AudioClip buzzSound;
        [SerializeField] private AudioClip stingSound;

        [Header("Visual")]
        [SerializeField] private GameObject beePrefab;
        [SerializeField] private Color beeColor = Color.yellow;

        // Runtime
        private List<Transform> bees = new List<Transform>();
        private Transform target;
        private AudioSource audioSource;
        private bool isAttacking = false;
        private float lastStingTime;
        private Coroutine attackCoroutine;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.loop = true;
            audioSource.spatialBlend = 1f; // 3D sound
        }

        private void Start()
        {
            CreateBees();
        }

        private void Update()
        {
            if (isAttacking && target != null)
            {
                // Move swarm center toward target
                Vector3 targetPos = target.position;
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 3f);

                // Animate individual bees
                AnimateBees();
            }
        }

        /// <summary>
        /// Set the number of bees in the swarm.
        /// </summary>
        public void SetBeeCount(int count)
        {
            beeCount = count;
            
            // Recreate bees if already spawned
            if (bees.Count > 0)
            {
                ClearBees();
                CreateBees();
            }
        }

        /// <summary>
        /// Start attacking a target for a duration.
        /// </summary>
        public void AttackTarget(Transform attackTarget, float duration)
        {
            target = attackTarget;
            isAttacking = true;

            // Start buzz sound
            if (buzzSound != null && audioSource != null)
            {
                audioSource.clip = buzzSound;
                audioSource.Play();
            }

            // Start attack coroutine
            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
            }
            attackCoroutine = StartCoroutine(AttackSequence(duration));

            Debug.Log($"[BeeSwarm] Attacking target for {duration} seconds with {beeCount} bees!");
        }

        /// <summary>
        /// Stop the attack and disperse.
        /// </summary>
        public void StopAttack()
        {
            isAttacking = false;
            
            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
            }

            // Stop sound
            if (audioSource != null)
            {
                audioSource.Stop();
            }

            // Disperse animation
            StartCoroutine(Disperse());
        }

        /// <summary>
        /// Dismiss the bee swarm (alias for StopAttack, used by PunishmentSystem).
        /// </summary>
        public void Dismiss()
        {
            Debug.Log("[BeeSwarm] Dismissed!");
            StopAttack();
        }

        private void CreateBees()
        {
            for (int i = 0; i < beeCount; i++)
            {
                GameObject bee;
                
                if (beePrefab != null)
                {
                    bee = Instantiate(beePrefab, transform);
                }
                else
                {
                    // Create simple bee representation
                    bee = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    bee.transform.SetParent(transform);
                    bee.transform.localScale = Vector3.one * 0.03f; // Small bee

                    // Set color
                    Renderer rend = bee.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        rend.material.color = beeColor;
                    }

                    // Remove collider (we don't need physics for visual bees)
                    Collider col = bee.GetComponent<Collider>();
                    if (col != null)
                    {
                        Destroy(col);
                    }
                }

                // Random starting position in sphere
                bee.transform.localPosition = Random.insideUnitSphere * swarmRadius;
                bees.Add(bee.transform);
            }
        }

        private void ClearBees()
        {
            foreach (Transform bee in bees)
            {
                if (bee != null)
                {
                    Destroy(bee.gameObject);
                }
            }
            bees.Clear();
        }

        private void AnimateBees()
        {
            float time = Time.time;

            for (int i = 0; i < bees.Count; i++)
            {
                if (bees[i] == null) continue;

                // Each bee has unique movement pattern
                float offset = i * 0.5f;
                
                // Orbit around center
                float angle = (time * orbitSpeed + offset) * Mathf.Deg2Rad * 100f;
                float radius = swarmRadius * (0.5f + 0.5f * Mathf.Sin(time * buzzSpeed + offset));
                
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(time * buzzSpeed * 2f + offset) * swarmRadius * 0.5f;
                float z = Mathf.Sin(angle) * radius;

                bees[i].localPosition = new Vector3(x, y, z);
            }
        }

        private IEnumerator AttackSequence(float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // Occasional sting effect
                if (Time.time - lastStingTime > stingInterval)
                {
                    lastStingTime = Time.time;
                    TriggerSting();
                }

                yield return null;
            }

            StopAttack();
        }

        private void TriggerSting()
        {
            // Play sting sound
            if (stingSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(stingSound, 0.5f);
            }

            Debug.Log("[BeeSwarm] Sting!");
        }

        private IEnumerator Disperse()
        {
            float disperseTime = 1f;
            float elapsed = 0f;

            Vector3[] directions = new Vector3[bees.Count];
            for (int i = 0; i < bees.Count; i++)
            {
                directions[i] = Random.onUnitSphere;
            }

            while (elapsed < disperseTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / disperseTime;

                for (int i = 0; i < bees.Count; i++)
                {
                    if (bees[i] != null)
                    {
                        bees[i].localPosition += directions[i] * Time.deltaTime * 5f;
                        bees[i].localScale = Vector3.one * 0.03f * (1f - t);
                    }
                }

                yield return null;
            }

            // Clean up
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            ClearBees();
        }
    }
}
