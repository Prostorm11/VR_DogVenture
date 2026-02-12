using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRProject.Events;

namespace VRProject.WordPuzzle
{
    /// <summary>
    /// A floating, grabbable letter cube for VR.
    /// Letters float in place and can be grabbed and rearranged.
    /// No gravity - letters stay where you put them.
    /// Uses material-based letter display (no TextMeshPro needed).
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public class FloatingLetter : MonoBehaviour
    {
        [Header("Letter Display - Material Based")]
        [Tooltip("Assign 26 materials, one for each letter A-Z. Each material should have the letter texture.")]
        [SerializeField] private Material[] letterMaterials; // A-Z materials (26 total)
        
        [Header("Letter Display - Alternative (Texture Atlas)")]
        [Tooltip("Or use a single material with UV offset per letter")]
        [SerializeField] private bool useTextureAtlas = false;
        [SerializeField] private Material atlasBaseMaterial;
        [SerializeField] private int atlasColumns = 6; // 6x5 grid for 26 letters
        [SerializeField] private int atlasRows = 5;

        [Header("Visual Settings")]
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color grabbedColor = new Color(0.5f, 0.8f, 1f); // Light blue
        [SerializeField] private Color correctColor = Color.green;
        [SerializeField] private Color incorrectColor = Color.red;

        [Header("Floating Animation")]
        [SerializeField] private bool enableFloatAnimation = true;
        [SerializeField] private float floatAmplitude = 0.02f;
        [SerializeField] private float floatSpeed = 1f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip grabSound;
        [SerializeField] private AudioClip releaseSound;

        // State
        private char letter;
        private XRGrabInteractable grabInteractable;
        private Rigidbody rb;
        private Vector3 originalScale;
        private Vector3 floatStartPosition;
        private float floatOffset;
        private bool isBeingHeld = false;
        private bool isInAnswerZone = false;
        private int answerSlotIndex = -1;
        private MaterialPropertyBlock propertyBlock;
        private Material instanceMaterial; // Instance of material to avoid sharing
        
        // Socket system state
        private LetterSocket currentSocket;
        private SubWordGameManager subWordManager;
        private Vector3 poolPosition;

        public char Letter => letter;
        public bool IsBeingHeld => isBeingHeld;
        public bool IsInAnswerZone => isInAnswerZone;
        public int AnswerSlotIndex => answerSlotIndex;
        public bool IsInSocket => currentSocket != null;
        public LetterSocket CurrentSocket => currentSocket;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            rb = GetComponent<Rigidbody>();
            originalScale = transform.localScale;
            propertyBlock = new MaterialPropertyBlock();

            // Configure Rigidbody for floating (no gravity)
            if (rb != null)
            {
                rb.useGravity = false;
                rb.linearDamping = 10f; // Higher damping for smoother stops
                rb.angularDamping = 10f;
                rb.interpolation = RigidbodyInterpolation.Interpolate; // Smoother movement
            }

            // Configure XR Grab Interactable for easier grabbing
            if (grabInteractable != null)
            {
                grabInteractable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
                grabInteractable.throwOnDetach = false; // Don't throw, just release
                grabInteractable.useDynamicAttach = true; // Grab from any point
                grabInteractable.matchAttachPosition = true;
                grabInteractable.matchAttachRotation = false; // Keep letter facing forward
                grabInteractable.snapToColliderVolume = false;
                
                grabInteractable.selectEntered.AddListener(OnGrabbed);
                grabInteractable.selectExited.AddListener(OnReleased);
            }

            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }

            // Random float offset for variety
            floatOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Start()
        {
            floatStartPosition = transform.position;
            SetColor(defaultColor);
        }

        private void Update()
        {
            // Gentle floating animation when not held
            if (enableFloatAnimation && !isBeingHeld)
            {
                float yOffset = Mathf.Sin((Time.time * floatSpeed) + floatOffset) * floatAmplitude;
                
                if (!isInAnswerZone)
                {
                    // Float around start position
                    Vector3 targetPos = floatStartPosition + new Vector3(0, yOffset, 0);
                    transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 2f);
                }
            }
        }

        /// <summary>
        /// Initialize the letter with a character.
        /// </summary>
        public void Initialize(char c)
        {
            letter = char.ToUpper(c);
            
            // Get letter index (A=0, B=1, ... Z=25)
            int letterIndex = letter - 'A';
            
            if (letterIndex < 0 || letterIndex > 25)
            {
                Debug.LogWarning($"Invalid letter character: {c}");
                return;
            }

            // Apply letter material
            if (meshRenderer != null)
            {
                if (useTextureAtlas && atlasBaseMaterial != null)
                {
                    // Use texture atlas with UV offset
                    ApplyAtlasLetter(letterIndex);
                }
                else if (letterMaterials != null && letterIndex < letterMaterials.Length && letterMaterials[letterIndex] != null)
                {
                    // Use individual letter materials
                    instanceMaterial = new Material(letterMaterials[letterIndex]);
                    meshRenderer.material = instanceMaterial;
                }
                else
                {
                    // Try to use the LetterTextureGenerator (auto-generated materials)
                    TryUseGeneratedMaterial(letter);
                }
            }

            gameObject.name = $"Letter_{letter}";
        }

        /// <summary>
        /// Try to get material from the LetterTextureGenerator.
        /// </summary>
        private void TryUseGeneratedMaterial(char letter)
        {
            if (LetterTextureGenerator.Instance != null)
            {
                Material genMat = LetterTextureGenerator.Instance.GetMaterialForLetter(letter);
                if (genMat != null)
                {
                    instanceMaterial = new Material(genMat);
                    meshRenderer.material = instanceMaterial;
                    return;
                }
            }
            
            Debug.LogWarning($"No material for letter {letter}. Add LetterTextureGenerator to scene, OR assign letterMaterials array (26 materials A-Z), OR use texture atlas.");
        }

        /// <summary>
        /// Apply letter from a texture atlas using UV offset.
        /// </summary>
        private void ApplyAtlasLetter(int letterIndex)
        {
            if (atlasBaseMaterial == null || meshRenderer == null) return;

            // Create instance material
            instanceMaterial = new Material(atlasBaseMaterial);
            meshRenderer.material = instanceMaterial;

            // Calculate UV offset for this letter in the atlas
            int col = letterIndex % atlasColumns;
            int row = letterIndex / atlasColumns;
            
            float tileX = 1f / atlasColumns;
            float tileY = 1f / atlasRows;
            
            // Set tiling and offset
            instanceMaterial.SetTextureScale("_MainTex", new Vector2(tileX, tileY));
            instanceMaterial.SetTextureScale("_BaseMap", new Vector2(tileX, tileY)); // URP
            
            instanceMaterial.SetTextureOffset("_MainTex", new Vector2(col * tileX, 1f - (row + 1) * tileY));
            instanceMaterial.SetTextureOffset("_BaseMap", new Vector2(col * tileX, 1f - (row + 1) * tileY)); // URP
        }

        /// <summary>
        /// Set the floating start position.
        /// </summary>
        public void SetFloatPosition(Vector3 position)
        {
            floatStartPosition = position;
            poolPosition = position; // Remember pool position
            transform.position = position;
            FaceCamera();
        }
        
        /// <summary>
        /// Make the letter face the camera so text is readable.
        /// </summary>
        private void FaceCamera()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 lookDir = cam.transform.position - transform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(-lookDir);
                }
            }
        }
        
        /// <summary>
        /// Link to the SubWordGameManager.
        /// </summary>
        public void SetSubWordManager(SubWordGameManager manager)
        {
            subWordManager = manager;
        }

        #region Grab Events

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            isBeingHeld = true;
            
            // Visual feedback
            SetColor(grabbedColor);
            transform.localScale = originalScale * 1.15f;
            
            // Stop any velocity
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // If was in a socket, notify it
            if (currentSocket != null)
            {
                currentSocket.OnLetterRemoved();
                currentSocket = null;
            }

            // If was in answer zone (old system), notify
            if (isInAnswerZone)
            {
                FloatingWordBuilder builder = FindAnyObjectByType<FloatingWordBuilder>();
                if (builder != null)
                {
                    builder.RemoveLetterFromAnswer(this);
                }
                isInAnswerZone = false;
                answerSlotIndex = -1;
            }

            PlaySound(grabSound);
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            isBeingHeld = false;
            
            // First try new socket system
            if (TrySnapToNearestSocket())
            {
                SetColor(defaultColor);
                transform.localScale = originalScale;
                PlaySound(releaseSound);
                return;
            }
            
            // Fall back to old answer zone system
            FloatingWordBuilder builder = FindAnyObjectByType<FloatingWordBuilder>();
            if (builder != null && builder.TrySnapLetterToAnswer(this))
            {
                // Successfully snapped to answer
                isInAnswerZone = true;
                SetColor(defaultColor);
            }
            else
            {
                // Released in open space - stay floating where released
                SetColor(defaultColor);
                floatStartPosition = transform.position;
                FaceCamera();
            }

            transform.localScale = originalScale;
            
            // Stop movement
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            PlaySound(releaseSound);
        }
        
        /// <summary>
        /// Try to snap to the nearest available socket.
        /// </summary>
        private bool TrySnapToNearestSocket()
        {
            LetterSocket[] sockets = FindObjectsByType<LetterSocket>(FindObjectsSortMode.None);
            
            LetterSocket nearestSocket = null;
            float nearestDist = 0.2f; // Max snap distance
            
            foreach (var socket in sockets)
            {
                if (socket.HasLetter) continue;
                
                float dist = Vector3.Distance(transform.position, socket.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestSocket = socket;
                }
            }
            
            if (nearestSocket != null)
            {
                return nearestSocket.TryPlaceLetter(this);
            }
            
            return false;
        }

        #endregion

        #region Visual Feedback

        public void SetColor(Color color)
        {
            if (meshRenderer != null)
            {
                // Use instance material if available
                if (instanceMaterial != null)
                {
                    instanceMaterial.SetColor("_BaseColor", color); // URP
                    instanceMaterial.SetColor("_Color", color); // Standard
                }
                else
                {
                    // Fallback to property block
                    meshRenderer.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetColor("_BaseColor", color); // URP
                    propertyBlock.SetColor("_Color", color); // Standard
                    meshRenderer.SetPropertyBlock(propertyBlock);
                }
            }
        }

        public void ShowCorrect()
        {
            SetColor(correctColor);
        }

        public void ShowIncorrect()
        {
            SetColor(incorrectColor);
            Invoke(nameof(ResetColor), 1f);
        }

        public void ResetColor()
        {
            SetColor(defaultColor);
        }

        /// <summary>
        /// Snap to a specific position (for answer zone).
        /// </summary>
        public void SnapToPosition(Vector3 position, int slotIndex)
        {
            transform.position = position;
            floatStartPosition = position;
            answerSlotIndex = slotIndex;
            isInAnswerZone = true;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// Return to original floating area using saved pool position.
        /// </summary>
        public void ReturnToPool()
        {
            ReturnToPool(poolPosition != Vector3.zero ? poolPosition : floatStartPosition);
        }

        /// <summary>
        /// Return to original floating area.
        /// </summary>
        public void ReturnToPool(Vector3 position)
        {
            isInAnswerZone = false;
            answerSlotIndex = -1;
            currentSocket = null;
            floatStartPosition = position;
            transform.position = position;
            ResetColor();
            FaceCamera();
        }
        
        /// <summary>
        /// Snap this letter to a socket.
        /// </summary>
        public void SnapToSocket(LetterSocket socket)
        {
            currentSocket = socket;
            transform.position = socket.transform.position;
            floatStartPosition = socket.transform.position;
            isInAnswerZone = false;
            
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            FaceCamera();
            Debug.Log($"[Letter] '{letter}' snapped to socket");
        }
        
        /// <summary>
        /// Clear from current socket and return to pool.
        /// </summary>
        public void ClearFromSocket()
        {
            currentSocket = null;
            
            if (poolPosition != Vector3.zero)
            {
                transform.position = poolPosition;
                floatStartPosition = poolPosition;
            }
            
            ResetColor();
            FaceCamera();
        }

        #endregion

        #region Audio

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        #endregion

        private void OnDestroy()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGrabbed);
                grabInteractable.selectExited.RemoveListener(OnReleased);
            }

            // Clean up instance material to prevent memory leaks
            if (instanceMaterial != null)
            {
                Destroy(instanceMaterial);
            }
        }
    }
}
