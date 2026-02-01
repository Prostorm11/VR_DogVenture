using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;
using VRDogVenture.Punishment;

namespace VRDogVenture.Game
{
    /// <summary>
    /// SIMPLE WORD GAME - Drop this on ANY object in your scene!
    /// Creates the entire sub-word game with floating letters and floating sockets.
    /// Includes bee punishment for wrong answers. NO submit button.
    /// Also makes the dog follow you.
    /// </summary>
    public class SimpleWordGame : MonoBehaviour
    {
        [Header("=== DRAG YOUR DOG HERE ===")]
        [SerializeField] private Transform dogTransform;
        
        [Header("Game Settings")]
        [SerializeField] private string[] baseWords = { "STAR", "TEAM", "STOP", "HEART", "STONE" };
        [SerializeField] private float letterSize = 0.12f; // Bigger letters
        [SerializeField] private float socketSize = 0.14f; // Bigger sockets
        
        [Header("Positions (Relative to player eye level)")]
        [SerializeField] private Vector3 letterSpawnOffset = new Vector3(0, 0.1f, 1.2f); // Slightly above eye level, in front
        [SerializeField] private Vector3 socketSpawnOffset = new Vector3(0, -0.2f, 1.2f); // Below letters (waist height)
        
        [Header("Floating Animation")]
        [SerializeField] private bool enableFloatAnimation = true;
        [SerializeField] private float floatAmplitude = 0.03f;
        [SerializeField] private float floatSpeed = 1.5f;
        
        [Header("Visual Settings")]
        [SerializeField] private Color letterColor = new Color(1f, 0.95f, 0.8f); // Warm white
        [SerializeField] private Color letterTextColor = new Color(0.2f, 0.15f, 0.1f); // Dark brown
        [SerializeField] private Color socketColor = new Color(0.3f, 0.7f, 1f, 0.4f); // Light blue glow
        [SerializeField] private Color socketFilledColor = new Color(0.5f, 1f, 0.5f, 0.4f); // Green when filled
        
        [Header("Bee Punishment")]
        [SerializeField] private bool enableBeePunishment = true;
        [SerializeField] private int wrongAnswersBeforeBees = 1;
        
        [Header("Dog Following Settings")]
        [SerializeField] private float dogSideOffset = 1.5f; // Slight offset to the side (reduced)
        [SerializeField] private float dogForwardOffset = 3.5f; // How far AHEAD of player (increased - dog leads!)
        [SerializeField] private float dogWalkSpeed = 2f;
        [SerializeField] private float dogRunSpeed = 5f;
        [SerializeField] private float dogStopThreshold = 1.5f; // Stop when within this distance
        [SerializeField] private float dogRunThreshold = 5f; // Start running when further than this
        
        [Header("Instructions")]
        [SerializeField] private bool showInstructions = true;
        private GameObject instructionsPanel;
        private bool instructionsVisible = true;
        
        // Valid sub-words for each base word
        private Dictionary<string, string[]> validSubWords = new Dictionary<string, string[]>()
        {
            { "STAR", new[] { "AT", "AS", "ART", "RAT", "TAR", "SAT", "STAR", "RATS", "ARTS", "TARS" } },
            { "TEAM", new[] { "AT", "AM", "ATE", "EAT", "MAT", "MET", "TEA", "MEAT", "MATE", "TAME", "TEAM" } },
            { "STOP", new[] { "TO", "SO", "TOP", "POT", "OPT", "SOT", "TOPS", "POTS", "SPOT", "STOP", "POST" } },
            { "HEART", new[] { "AT", "ATE", "EAT", "EAR", "ART", "HAT", "RAT", "THE", "HEAT", "RATE", "HATE", "HEAR", "EARTH", "HEART" } },
            { "STONE", new[] { "TO", "ON", "NO", "SO", "ONE", "TON", "TEN", "SET", "NET", "NOT", "TONE", "NOSE", "NOTE", "ONES", "TONES", "STONE", "NOTES" } },
        };
        
        // Runtime
        private Transform player;
        private List<GameObject> letterObjects = new List<GameObject>();
        private List<GameObject> socketObjects = new List<GameObject>();
        private List<char> socketsContent = new List<char>();
        private string currentBaseWord;
        private int currentWordIndex = 0;
        private int currentSlotCount = 2; // Start with 2-letter words
        private int score = 0;
        private HashSet<string> foundWords = new HashSet<string>();
        private int consecutiveWrongAnswers = 0;
        private Coroutine clearCoroutine;
        private Coroutine nextChallengeCoroutine;
        private Vector3 gameZoneCenter; // Store the center of the play area
        private Vector3 gameZoneRight; // Store the right direction for layout
        private float initialPlayerY; // Store initial player height to prevent drift
        
        // UI
        private TextMeshPro baseWordText;
        private TextMeshPro instructionText;
        private TextMeshPro scoreText;
        private TextMeshPro feedbackText;
        
        // Dog following
        private bool dogFollowing = true;
        private Vector3 dogTargetPos;
        private Animator dogAnimator;
        private ithappy.Animals_FREE.CreatureMover dogCreatureMover;
        private bool dogIsMoving = false;
        
        private void Start()
        {
            Debug.Log("===== SIMPLE WORD GAME STARTING =====");
            
            // Find player camera
            Camera cam = Camera.main;
            if (cam != null)
            {
                player = cam.transform;
                // Store initial player height - this will be used for ALL position calculations
                // to prevent height drift when player moves head up/down in VR
                initialPlayerY = player.position.y;
                Debug.Log($"[SimpleWordGame] Found player: {player.name}, initial Y height: {initialPlayerY}");
            }
            else
            {
                Debug.LogError("[SimpleWordGame] No main camera found!");
                return;
            }
            
            // Find dog if not assigned
            if (dogTransform == null)
            {
                GameObject dog = GameObject.Find("Dog_001");
                if (dog == null) dog = GameObject.Find("Dog");
                if (dog == null) dog = GameObject.Find("DogCompanion");
                if (dog != null)
                {
                    dogTransform = dog.transform;
                    Debug.Log($"[SimpleWordGame] Found dog: {dogTransform.name}");
                }
            }
            
            // Get dog animator and CreatureMover for animations
            if (dogTransform != null)
            {
                dogAnimator = dogTransform.GetComponentInChildren<Animator>();
                dogCreatureMover = dogTransform.GetComponent<ithappy.Animals_FREE.CreatureMover>();
                if (dogCreatureMover == null)
                    dogCreatureMover = dogTransform.GetComponentInChildren<ithappy.Animals_FREE.CreatureMover>();
                    
                Debug.Log($"[SimpleWordGame] Dog Animator: {(dogAnimator != null ? "Found" : "NOT FOUND")}");
                Debug.Log($"[SimpleWordGame] Dog CreatureMover: {(dogCreatureMover != null ? "Found" : "NOT FOUND")}");
            }
            
            // Disable old systems
            DisableOldSystems();
            
            // Setup bee punishment system
            SetupPunishmentSystem();
            
            // Create UI
            CreateUI();
            
            // Start first word
            StartNewWord();
            
            Debug.Log("===== SIMPLE WORD GAME READY =====");
        }
        
        private void Update()
        {
            // Update dog following
            if (dogTransform != null && player != null && dogFollowing)
            {
                UpdateDogFollow();
            }
            
            // Check sockets when letters change
            CheckSockets();
        }
        
        private void DisableOldSystems()
        {
            // Find and disable FloatingWordBuilder
            var oldBuilders = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var mb in oldBuilders)
            {
                if (mb.GetType().Name == "FloatingWordBuilder")
                {
                    mb.enabled = false;
                    Debug.Log($"[SimpleWordGame] Disabled old FloatingWordBuilder on {mb.gameObject.name}");
                    
                    // Hide its children (submit button, etc)
                    foreach (Transform child in mb.transform)
                    {
                        child.gameObject.SetActive(false);
                    }
                }
                
                // Also disable SubWordGameManager if it exists
                if (mb.GetType().Name == "SubWordGameManager")
                {
                    mb.enabled = false;
                    Debug.Log($"[SimpleWordGame] Disabled SubWordGameManager");
                }
                
                // Disable DogARGuideController
                if (mb.GetType().Name == "DogARGuideController")
                {
                    mb.enabled = false;
                    Debug.Log($"[SimpleWordGame] Disabled DogARGuideController");
                }
                
                // Disable DogCompanion - we handle dog movement ourselves
                if (mb.GetType().Name == "DogCompanion")
                {
                    mb.enabled = false;
                    Debug.Log($"[SimpleWordGame] Disabled DogCompanion (using SimpleWordGame dog control)");
                }
                
                // DON'T disable CreatureMover - we need it for animations!
                // The CreatureMover.SetCommand with move=false lets us animate without it moving the dog
            }
            
            // Find and destroy/hide submit buttons
            DisableSubmitButtons();
        }
        
        private void DisableSubmitButtons()
        {
            // Find all buttons with "submit" in the name
            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                string nameLower = obj.name.ToLower();
                if (nameLower.Contains("submit") || nameLower.Contains("check") || nameLower.Contains("confirm"))
                {
                    obj.SetActive(false);
                    Debug.Log($"[SimpleWordGame] Hidden submit button: {obj.name}");
                }
            }
            
            // Also look for any canvas buttons
            var buttons = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);
            foreach (var btn in buttons)
            {
                string nameLower = btn.gameObject.name.ToLower();
                if (nameLower.Contains("submit") || nameLower.Contains("check"))
                {
                    btn.gameObject.SetActive(false);
                    Debug.Log($"[SimpleWordGame] Hidden UI button: {btn.gameObject.name}");
                }
            }
        }
        
        private void SetupPunishmentSystem()
        {
            if (!enableBeePunishment) return;
            
            // Create PunishmentSystem if it doesn't exist
            if (PunishmentSystem.Instance == null)
            {
                GameObject psObj = new GameObject("PunishmentSystem");
                psObj.AddComponent<PunishmentSystem>();
                Debug.Log("[SimpleWordGame] Created PunishmentSystem for bee punishment");
            }
            else
            {
                Debug.Log("[SimpleWordGame] PunishmentSystem already exists");
            }
        }
        
        private void CreateUI()
        {
            Vector3 forward = player.forward;
            forward.y = 0;
            forward.Normalize();
            
            // UI at same distance as game elements, using fixed initial Y height
            Vector3 fixedPlayerPos = new Vector3(player.position.x, initialPlayerY, player.position.z);
            Vector3 uiPos = fixedPlayerPos + forward * 1.2f;
            
            // Base word display - above the letters
            GameObject baseWordObj = new GameObject("BaseWordText");
            baseWordObj.transform.position = uiPos + Vector3.up * 0.35f; // Just above eye level
            baseWordText = baseWordObj.AddComponent<TextMeshPro>();
            baseWordText.fontSize = 1.5f;
            baseWordText.alignment = TextAlignmentOptions.Center;
            baseWordText.color = Color.yellow;
            baseWordText.text = "LOADING...";
            FaceCamera(baseWordObj.transform);
            
            // Instruction text (short version above game)
            GameObject instrObj = new GameObject("InstructionText");
            instrObj.transform.position = uiPos + Vector3.up * 0.2f; // Below base word
            instructionText = instrObj.AddComponent<TextMeshPro>();
            instructionText.fontSize = 0.5f;
            instructionText.alignment = TextAlignmentOptions.Center;
            instructionText.color = Color.cyan;
            instructionText.text = "Grab letters and drop them in the slots!";
            FaceCamera(instrObj.transform);
            
            // Create full instructions panel
            if (showInstructions)
            {
                CreateInstructionsPanel(uiPos);
            }
            
            // Score text - below sockets
            GameObject scoreObj = new GameObject("ScoreText");
            scoreObj.transform.position = uiPos + Vector3.up * -0.45f;
            scoreText = scoreObj.AddComponent<TextMeshPro>();
            scoreText.fontSize = 0.4f;
            scoreText.alignment = TextAlignmentOptions.Center;
            scoreText.color = Color.green;
            scoreText.text = "Score: 0";
            FaceCamera(scoreObj.transform);
            
            // Feedback text - between letters and sockets
            GameObject feedbackObj = new GameObject("FeedbackText");
            feedbackObj.transform.position = uiPos + Vector3.up * -0.05f;
            feedbackText = feedbackObj.AddComponent<TextMeshPro>();
            feedbackText.fontSize = 0.6f;
            feedbackText.alignment = TextAlignmentOptions.Center;
            feedbackText.color = Color.white;
            feedbackText.text = "";
            FaceCamera(feedbackObj.transform);
            
            // Create Instructions Toggle Button
            CreateInstructionsToggleButton(uiPos);
        }
        
        private void CreateInstructionsPanel(Vector3 uiPos)
        {
            // Create instructions panel to the RIGHT of the game area
            instructionsPanel = new GameObject("InstructionsPanel");
            Vector3 rightOffset = player.right * 0.6f;
            instructionsPanel.transform.position = uiPos + rightOffset + Vector3.up * -0.1f; // At eye level, to the side
            
            // Background panel
            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "InstructionsBG";
            bg.transform.SetParent(instructionsPanel.transform);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = new Vector3(0.5f, 0.6f, 1f);
            Destroy(bg.GetComponent<Collider>());
            
            Material bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (bgMat.shader == null) bgMat = new Material(Shader.Find("Unlit/Color"));
            bgMat.color = new Color(0.1f, 0.15f, 0.25f, 0.9f);
            bg.GetComponent<Renderer>().material = bgMat;
            
            // Title
            CreatePanelText(instructionsPanel.transform, "HOW TO PLAY", 
                new Vector3(0, 0.22f, -0.01f), 0.06f, Color.yellow);
            
            // Instructions list
            string[] instructions = new string[]
            {
                "1. Look at the BASE WORD above",
                "2. GRAB floating letters with your hands",
                "3. DROP letters into the BLUE SLOTS",
                "4. Form a valid word using those letters",
                "5. Word is checked automatically!",
                "",
                "TIPS:",
                "• Start with short words (2-3 letters)",
                "• Each letter can only be used once",
                "• Dog will BARK when you get it right!",
                "• Wrong answers bring the BEES!"
            };
            
            float yPos = 0.15f;
            foreach (string line in instructions)
            {
                Color lineColor = line.StartsWith("•") ? Color.cyan : 
                                 line.StartsWith("TIPS") ? Color.yellow : Color.white;
                float fontSize = line.StartsWith("TIPS") ? 0.035f : 0.028f;
                
                CreatePanelText(instructionsPanel.transform, line, 
                    new Vector3(0, yPos, -0.01f), fontSize, lineColor);
                yPos -= 0.035f;
            }
            
            FaceCamera(instructionsPanel.transform);
            instructionsVisible = true;
        }
        
        private void CreatePanelText(Transform parent, string text, Vector3 localPos, float fontSize, Color color)
        {
            GameObject textObj = new GameObject($"Text_{text.Substring(0, Mathf.Min(10, text.Length))}");
            textObj.transform.SetParent(parent);
            textObj.transform.localPosition = localPos;
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = Vector3.one;
            
            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.rectTransform.sizeDelta = new Vector2(0.48f, 0.05f);
        }
        
        private void CreateInstructionsToggleButton(Vector3 uiPos)
        {
            // Create toggle button below the game
            GameObject btnObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            btnObj.name = "ToggleInstructionsBtn";
            btnObj.transform.position = uiPos + Vector3.up * 0.5f + player.right * 0.25f;
            btnObj.transform.localScale = new Vector3(0.12f, 0.04f, 0.02f);
            
            Material btnMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (btnMat.shader == null) btnMat = new Material(Shader.Find("Standard"));
            btnMat.color = new Color(0.3f, 0.5f, 0.8f);
            btnObj.GetComponent<Renderer>().material = btnMat;
            
            // Button text
            GameObject textObj = new GameObject("BtnText");
            textObj.transform.SetParent(btnObj.transform);
            textObj.transform.localPosition = new Vector3(0, 0, -0.6f);
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = Vector3.one * 15f;
            
            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = "?";
            tmp.fontSize = 2f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            
            // Make it interactable
            XRSimpleInteractable interactable = btnObj.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener((args) => ToggleInstructions());
            
            FaceCamera(btnObj.transform);
        }
        
        public void ToggleInstructions()
        {
            if (instructionsPanel != null)
            {
                instructionsVisible = !instructionsVisible;
                instructionsPanel.SetActive(instructionsVisible);
                Debug.Log($"[SimpleWordGame] Instructions {(instructionsVisible ? "shown" : "hidden")}");
            }
            else if (showInstructions)
            {
                // Create panel if it doesn't exist
                Vector3 forward = player.forward;
                forward.y = 0;
                forward.Normalize();
                Vector3 uiPos = player.position + forward * 1.2f;
                CreateInstructionsPanel(uiPos);
            }
        }
        
        private void StartNewWord()
        {
            // Clear existing
            ClearAll();
            
            // Calculate game zone position based on current player location
            Vector3 forward = player.forward;
            forward.y = 0;
            forward.Normalize();
            
            // Store game zone - use player X/Z but FIXED Y height from game start
            // This prevents height drift when player moves head up/down in VR
            Vector3 fixedPlayerPos = new Vector3(player.position.x, initialPlayerY, player.position.z);
            gameZoneCenter = fixedPlayerPos + forward * letterSpawnOffset.z;
            gameZoneRight = player.right;
            gameZoneRight.y = 0;
            gameZoneRight.Normalize();
            
            Debug.Log($"[SimpleWordGame] Game zone set at: {gameZoneCenter} (player at {player.position}, initialY={initialPlayerY})");
            
            // Get base word
            currentBaseWord = baseWords[currentWordIndex % baseWords.Length];
            foundWords.Clear();
            currentSlotCount = 2; // Start with 2-letter words (easier!)
            
            // Update UI
            if (baseWordText != null) baseWordText.text = currentBaseWord;
            if (instructionText != null) instructionText.text = $"Make a {currentSlotCount}-letter word!";
            
            // Spawn letters and sockets using the stored game zone
            SpawnLetters();
            SpawnSockets();
            
            Debug.Log($"[SimpleWordGame] Started word: {currentBaseWord}");
        }
        
        private void ClearAll()
        {
            foreach (var obj in letterObjects)
            {
                if (obj != null) Destroy(obj);
            }
            letterObjects.Clear();
            
            foreach (var obj in socketObjects)
            {
                if (obj != null) Destroy(obj);
            }
            socketObjects.Clear();
            socketsContent.Clear();
        }
        
        private void SpawnLetters()
        {
            // Use stored game zone position + letter height offset
            Vector3 center = gameZoneCenter + Vector3.up * letterSpawnOffset.y;
            
            float spacing = letterSize * 1.5f;
            float totalWidth = (currentBaseWord.Length - 1) * spacing;
            Vector3 startPos = center - gameZoneRight * (totalWidth / 2f);
            
            for (int i = 0; i < currentBaseWord.Length; i++)
            {
                char c = currentBaseWord[i];
                Vector3 pos = startPos + gameZoneRight * (i * spacing);
                
                GameObject letter = CreateLetterCube(c, pos);
                letterObjects.Add(letter);
            }
        }
        
        private GameObject CreateLetterCube(char letter, Vector3 position)
        {
            // Create parent object for letter
            GameObject letterParent = new GameObject($"Letter_{letter}");
            letterParent.transform.position = position;
            
            // Create cube
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "LetterVisual";
            cube.transform.SetParent(letterParent.transform);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale = Vector3.one * letterSize;
            
            // Remove default collider, add larger trigger for easier grabbing
            Destroy(cube.GetComponent<BoxCollider>());
            BoxCollider grabCollider = letterParent.AddComponent<BoxCollider>();
            grabCollider.size = Vector3.one * letterSize * 1.5f;
            
            // Try to use LetterTextureGenerator for nice material-based letters (like original)
            Renderer rend = cube.GetComponent<Renderer>();
            bool usedGenerator = false;
            
            if (VRDogVenture.WordPuzzle.LetterTextureGenerator.Instance != null)
            {
                Material genMat = VRDogVenture.WordPuzzle.LetterTextureGenerator.Instance.GetMaterialForLetter(letter);
                if (genMat != null)
                {
                    rend.material = new Material(genMat);
                    usedGenerator = true;
                    Debug.Log($"[SimpleWordGame] Using LetterTextureGenerator for '{letter}'");
                }
            }
            
            if (!usedGenerator)
            {
                // Fallback: Create nice material with TextMeshPro
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.shader == null) mat = new Material(Shader.Find("Standard"));
                mat.color = letterColor;
                mat.SetFloat("_Smoothness", 0.6f);
                rend.material = mat;
                
                // Add text on front face
                GameObject textFront = new GameObject("LetterTextFront");
                textFront.transform.SetParent(cube.transform);
                textFront.transform.localPosition = new Vector3(0, 0, -0.52f);
                textFront.transform.localRotation = Quaternion.identity;
                textFront.transform.localScale = Vector3.one * 8f;
                
                TextMeshPro tmpFront = textFront.AddComponent<TextMeshPro>();
                tmpFront.text = letter.ToString();
                tmpFront.fontSize = 4f;
                tmpFront.alignment = TextAlignmentOptions.Center;
                tmpFront.color = letterTextColor;
                tmpFront.fontStyle = FontStyles.Bold;
                
                // Add text on back face too
                GameObject textBack = new GameObject("LetterTextBack");
                textBack.transform.SetParent(cube.transform);
                textBack.transform.localPosition = new Vector3(0, 0, 0.52f);
                textBack.transform.localRotation = Quaternion.Euler(0, 180, 0);
                textBack.transform.localScale = Vector3.one * 8f;
                
                TextMeshPro tmpBack = textBack.AddComponent<TextMeshPro>();
                tmpBack.text = letter.ToString();
                tmpBack.fontSize = 4f;
                tmpBack.alignment = TextAlignmentOptions.Center;
                tmpBack.color = letterTextColor;
                tmpBack.fontStyle = FontStyles.Bold;
            }
            
            // Rigidbody for physics (on parent)
            Rigidbody rb = letterParent.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearDamping = 10f;
            rb.angularDamping = 10f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            
            // XR Grab Interactable
            XRGrabInteractable grab = letterParent.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.throwOnDetach = false;
            grab.useDynamicAttach = true;
            grab.matchAttachPosition = true;
            grab.matchAttachRotation = false; // Keep letters upright
            
            // Store letter data
            SimpleLetter letterData = letterParent.AddComponent<SimpleLetter>();
            letterData.letter = letter;
            letterData.game = this;
            letterData.originalPosition = position;
            letterData.enableFloating = enableFloatAnimation;
            letterData.floatAmplitude = floatAmplitude;
            letterData.floatSpeed = floatSpeed;
            letterData.floatOffset = Random.Range(0f, Mathf.PI * 2f);
            
            // Face camera
            FaceCamera(letterParent.transform);
            
            return letterParent;
        }
        
        private void SpawnSockets()
        {
            Debug.Log($"[SimpleWordGame] SpawnSockets called. currentSlotCount={currentSlotCount}");
            
            socketsContent.Clear();
            
            // Use stored game zone position + socket height offset
            Vector3 center = gameZoneCenter + Vector3.up * socketSpawnOffset.y;
            
            float spacing = socketSize * 1.3f;
            float totalWidth = (currentSlotCount - 1) * spacing;
            Vector3 startPos = center - gameZoneRight * (totalWidth / 2f);
            
            for (int i = 0; i < currentSlotCount; i++)
            {
                Vector3 pos = startPos + gameZoneRight * (i * spacing);
                
                GameObject socket = CreateSocket(i, pos);
                socketObjects.Add(socket);
                socketsContent.Add('\0'); // Empty
            }
            
            Debug.Log($"[SimpleWordGame] SpawnSockets complete. Created {socketObjects.Count} sockets at center {center}");
        }
        
        private GameObject CreateSocket(int index, Vector3 position)
        {
            // Create socket parent
            GameObject socket = new GameObject($"Socket_{index}");
            socket.transform.position = position;
            Debug.Log($"[SimpleWordGame] Creating socket {index} at position {position}");
            
            // Create glowing frame visual
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "SocketFrame";
            frame.transform.SetParent(socket.transform);
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localScale = Vector3.one * socketSize;
            
            // Create transparent glowing material - use proper shader finding
            Renderer rend = frame.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default"); // Ultimate fallback
            
            Material mat = new Material(shader);
            
            // Make it transparent
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.color = socketColor;
            rend.material = mat;
            
            // Create inner glow particles/indicator (simpler - just a small sphere in center)
            GameObject innerGlow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            innerGlow.name = "InnerGlow";
            innerGlow.transform.SetParent(socket.transform);
            innerGlow.transform.localPosition = Vector3.zero;
            innerGlow.transform.localScale = Vector3.one * socketSize * 0.3f;
            Destroy(innerGlow.GetComponent<Collider>());
            
            Shader glowShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (glowShader == null) glowShader = Shader.Find("Unlit/Color");
            if (glowShader == null) glowShader = Shader.Find("Sprites/Default");
            Material glowMat = new Material(glowShader);
            glowMat.color = new Color(0.5f, 0.8f, 1f, 0.6f);
            innerGlow.GetComponent<Renderer>().material = glowMat;
            
            // Collider for trigger detection (larger area)
            BoxCollider col = frame.GetComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = Vector3.one * 2.5f; // Even larger trigger area for easier snapping
            
            // Socket script
            SimpleSocket socketScript = socket.AddComponent<SimpleSocket>();
            socketScript.index = index;
            socketScript.game = this;
            
            // Face camera
            FaceCamera(socket.transform);
            
            return socket;
        }
        
        public void OnLetterEnteredSocket(int socketIndex, char letter, GameObject letterObj)
        {
            if (socketIndex < 0 || socketIndex >= socketsContent.Count) return;
            
            // Check if socket is already occupied by a DIFFERENT letter
            if (socketsContent[socketIndex] != '\0' && socketsContent[socketIndex] != letter)
            {
                Debug.Log($"[SimpleWordGame] Socket {socketIndex} already has letter '{socketsContent[socketIndex]}' - finding another letter to displace");
                
                // Find and displace the letter currently in this socket
                foreach (var existingLetterObj in letterObjects)
                {
                    SimpleLetter existingLetter = existingLetterObj?.GetComponent<SimpleLetter>();
                    if (existingLetter != null && existingLetter.currentSocketIndex == socketIndex)
                    {
                        // Return this letter to its original position
                        existingLetter.currentSocketIndex = -1;
                        break;
                    }
                }
                
                socketsContent[socketIndex] = '\0'; // Clear the socket
            }
            
            // Place letter
            socketsContent[socketIndex] = letter;
            
            // Snap letter to socket position
            letterObj.transform.position = socketObjects[socketIndex].transform.position;
            
            // Stop physics
            Rigidbody rb = letterObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true; // Freeze in place
            }
            
            // Change socket color to show it's filled
            Renderer socketRend = socketObjects[socketIndex].GetComponentInChildren<Renderer>();
            if (socketRend != null)
            {
                socketRend.material.color = new Color(0.5f, 0.8f, 0.5f, 0.5f); // Greenish when filled
            }
            
            Debug.Log($"[SimpleWordGame] Letter '{letter}' placed in socket {socketIndex}");
            
            // Check if all sockets filled
            CheckWord();
        }
        
        public void OnLetterExitedSocket(int socketIndex, char letter)
        {
            if (socketIndex < 0 || socketIndex >= socketsContent.Count) return;
            
            if (socketsContent[socketIndex] == letter)
            {
                socketsContent[socketIndex] = '\0';
                
                // Reset socket color
                Renderer socketRend = socketObjects[socketIndex].GetComponentInChildren<Renderer>();
                if (socketRend != null)
                {
                    socketRend.material.color = socketColor; // Back to original blue
                }
                
                // Re-enable physics on the letter
                foreach (var letterObj in letterObjects)
                {
                    SimpleLetter sl = letterObj?.GetComponent<SimpleLetter>();
                    if (sl != null && sl.letter == letter && sl.currentSocketIndex < 0)
                    {
                        Rigidbody rb = letterObj.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.isKinematic = false;
                        }
                        break;
                    }
                }
                
                Debug.Log($"[SimpleWordGame] Letter '{letter}' removed from socket {socketIndex}");
            }
        }
        
        private void CheckSockets()
        {
            // This is called every frame to check socket states
            // The actual word checking is done in CheckWord when all sockets are filled
        }
        
        private void CheckWord()
        {
            // Check if all sockets have letters
            foreach (char c in socketsContent)
            {
                if (c == '\0') return; // Not all filled
            }
            
            // Build word from sockets
            string word = new string(socketsContent.ToArray());
            Debug.Log($"[SimpleWordGame] Checking word: {word}");
            
            // Check if valid
            if (IsValidWord(word))
            {
                OnCorrectWord(word);
            }
            else
            {
                OnIncorrectWord(word);
            }
        }
        
        private bool IsValidWord(string word)
        {
            if (!validSubWords.ContainsKey(currentBaseWord)) return false;
            
            string[] valid = validSubWords[currentBaseWord];
            foreach (string v in valid)
            {
                if (v.Equals(word, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        
        private void OnCorrectWord(string word)
        {
            if (foundWords.Contains(word.ToUpper()))
            {
                ShowFeedback("Already found!", Color.yellow);
                ResetLettersToStart();
                return;
            }
            
            foundWords.Add(word.ToUpper());
            
            // Cancel any pending operations
            CancelInvoke();
            if (clearCoroutine != null) { StopCoroutine(clearCoroutine); clearCoroutine = null; }
            if (nextChallengeCoroutine != null) { StopCoroutine(nextChallengeCoroutine); nextChallengeCoroutine = null; }
            
            // Stop bees on correct answer!
            StopBeePunishment();
            consecutiveWrongAnswers = 0;
            
            // Award points
            int points = word.Length * 10 + 25;
            score += points;
            if (scoreText != null) scoreText.text = $"Score: {score}";
            
            // Show feedback
            ShowFeedback($"CORRECT! +{points}", Color.green);
            
            // Color sockets green
            foreach (var socket in socketObjects)
            {
                if (socket != null)
                {
                    Renderer rend = socket.GetComponentInChildren<Renderer>();
                    if (rend != null) rend.material.color = socketFilledColor;
                }
            }
            
            Debug.Log($"[SimpleWordGame] CORRECT! Word: {word}, Points: {points}");
            
            // Dog bark (if exists)
            if (dogTransform != null) DogBark();
            
            // Schedule next challenge using simple Invoke
            Invoke(nameof(DoNextChallenge), 1.5f);
        }
        
        private void OnIncorrectWord(string word)
        {
            ShowFeedback("WRONG! Try again", Color.red);
            
            // Color sockets red
            foreach (var socket in socketObjects)
            {
                if (socket != null)
                {
                    Renderer rend = socket.GetComponentInChildren<Renderer>();
                    if (rend != null) rend.material.color = new Color(1f, 0.3f, 0.3f, 0.5f);
                }
            }
            
            Debug.Log($"[SimpleWordGame] INCORRECT! '{word}' is not valid");
            
            // Track wrong answers for bee punishment
            consecutiveWrongAnswers++;
            
            // Trigger bee punishment!
            if (enableBeePunishment && consecutiveWrongAnswers >= wrongAnswersBeforeBees)
            {
                TriggerBeePunishment();
            }
            
            // Reset letters after delay
            Invoke(nameof(ResetLettersToStart), 1f);
        }
        
        private void TriggerBeePunishment()
        {
            Debug.Log("[SimpleWordGame] Triggering bee punishment!");
            if (PunishmentSystem.Instance != null)
            {
                PunishmentSystem.Instance.StartContinuousPunishment();
            }
        }
        
        private void StopBeePunishment()
        {
            if (PunishmentSystem.Instance != null)
            {
                PunishmentSystem.Instance.StopContinuousPunishment();
            }
        }
        
        private void ResetLettersToStart()
        {
            // Reset socket colors
            foreach (var socket in socketObjects)
            {
                if (socket != null)
                {
                    Renderer rend = socket.GetComponentInChildren<Renderer>();
                    if (rend != null) rend.material.color = socketColor;
                }
            }
            
            // Return letters to original positions
            foreach (var letterObj in letterObjects)
            {
                if (letterObj != null)
                {
                    SimpleLetter letter = letterObj.GetComponent<SimpleLetter>();
                    if (letter != null)
                    {
                        letterObj.transform.position = letter.originalPosition;
                        letter.currentSocketIndex = -1;
                        Rigidbody rb = letterObj.GetComponent<Rigidbody>();
                        if (rb != null) rb.isKinematic = false;
                    }
                }
            }
            
            // Clear socket content
            for (int i = 0; i < socketsContent.Count; i++)
            {
                socketsContent[i] = '\0';
            }
        }
        
        /// <summary>
        /// SIMPLE next challenge - no coroutines, just direct method call via Invoke
        /// </summary>
        private void DoNextChallenge()
        {
            Debug.Log($"[SimpleWordGame] DoNextChallenge called. SlotCount={currentSlotCount}, Word={currentBaseWord}");
            
            // Increase slot count
            currentSlotCount++;
            
            // Check if we need a new word
            if (string.IsNullOrEmpty(currentBaseWord) || currentSlotCount > currentBaseWord.Length)
            {
                // Move to next word
                currentWordIndex++;
                if (currentWordIndex >= baseWords.Length)
                {
                    ShowFeedback("ALL WORDS COMPLETE!", Color.yellow);
                    currentWordIndex = 0;
                }
                
                // Start completely fresh with new word
                StartNewWord();
                return;
            }
            
            // Same word, just more sockets - SIMPLE APPROACH
            Debug.Log($"[SimpleWordGame] Spawning {currentSlotCount} sockets for same word");
            
            // 1. Destroy old sockets
            foreach (var socket in socketObjects)
            {
                if (socket != null) Destroy(socket);
            }
            socketObjects.Clear();
            socketsContent.Clear();
            
            // 2. Reset letters
            ResetLettersToStart();
            
            // 3. Spawn new sockets (uses stored gameZoneCenter)
            SpawnSockets();
            
            // 4. Update instruction
            if (instructionText != null)
            {
                instructionText.text = $"Make a {currentSlotCount}-letter word!";
            }
            
            Debug.Log($"[SimpleWordGame] Challenge ready: {currentSlotCount} sockets, {socketObjects.Count} created");
        }
        
        private void ShowFeedback(string message, Color color)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = color;
                StartCoroutine(ClearFeedback());
            }
        }
        
        private IEnumerator ClearFeedback()
        {
            yield return new WaitForSeconds(2f);
            if (feedbackText != null)
            {
                feedbackText.text = "";
            }
        }
        
        private void FaceCamera(Transform t)
        {
            if (player != null)
            {
                Vector3 lookDir = player.position - t.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    t.rotation = Quaternion.LookRotation(-lookDir);
                }
            }
        }
        
        #region Dog Following
        
        private void UpdateDogFollow()
        {
            // Calculate target position - IN FRONT of player (dog leads the way!)
            Vector3 sideOffset = -player.right * dogSideOffset; // Slight offset to side
            Vector3 forwardOffset = player.forward * dogForwardOffset; // Mostly ahead
            dogTargetPos = player.position + sideOffset + forwardOffset;
            dogTargetPos.y = dogTransform.position.y; // Keep on ground
            
            float distance = Vector3.Distance(dogTransform.position, dogTargetPos);
            Vector3 direction = (dogTargetPos - dogTransform.position).normalized;
            
            // Determine if dog should move
            if (distance > dogStopThreshold)
            {
                // Dog needs to move
                bool wasMoving = dogIsMoving;
                dogIsMoving = true;
                
                bool shouldRun = distance > dogRunThreshold;
                float speed = shouldRun ? dogRunSpeed : dogWalkSpeed;
                
                // Move dog manually (don't rely on CreatureMover's movement)
                dogTransform.position += direction * speed * Time.deltaTime;
                
                // Rotate to face movement direction
                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(direction);
                    dogTransform.rotation = Quaternion.Slerp(dogTransform.rotation, targetRot, 5f * Time.deltaTime);
                }
                
                // ANIMATION - Try multiple approaches
                float animSpeed = shouldRun ? 1.0f : 0.5f;
                
                // Log state change
                if (!wasMoving)
                {
                    Debug.Log($"[SimpleWordGame] Dog started {(shouldRun ? "RUNNING" : "WALKING")} - distance: {distance:F2}");
                }
                
                // Method 1: Direct animator control (most reliable)
                if (dogAnimator != null)
                {
                    // The ithappy Dog.controller uses "Vert" for forward movement and "State" for walk/run blend
                    dogAnimator.SetFloat("Vert", animSpeed * 2f); // Multiply for more visible leg movement
                    dogAnimator.SetFloat("State", shouldRun ? 1f : 0.3f);
                }
                
                // Method 2: CreatureMover (if animator didn't work)
                if (dogCreatureMover != null)
                {
                    // Tell CreatureMover to animate but NOT move (we handle movement)
                    dogCreatureMover.SetCommand(new Vector2(0, animSpeed), dogTargetPos, shouldRun, false);
                }
            }
            else
            {
                // Dog is close enough - stop and look at player
                if (dogIsMoving)
                {
                    Debug.Log("[SimpleWordGame] Dog stopped moving - close to target");
                }
                dogIsMoving = false;
                
                // Face the player
                Vector3 lookDir = player.position - dogTransform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir);
                    dogTransform.rotation = Quaternion.Slerp(dogTransform.rotation, targetRot, 3f * Time.deltaTime);
                }
                
                // ANIMATION: Idle
                if (dogAnimator != null)
                {
                    dogAnimator.SetFloat("Vert", 0f);
                    dogAnimator.SetFloat("State", 0f);
                }
                
                if (dogCreatureMover != null)
                {
                    dogCreatureMover.SetCommand(Vector2.zero, player.position, false, false);
                }
            }
        }
        
        private void DogBark()
        {
            // Create bark popup
            GameObject popup = new GameObject("BarkPopup");
            popup.transform.position = dogTransform.position + Vector3.up * 0.5f;
            
            TextMeshPro tmp = popup.AddComponent<TextMeshPro>();
            tmp.text = "WOOF!";
            tmp.fontSize = 2f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.yellow;
            
            FaceCamera(popup.transform);
            
            Destroy(popup, 1f);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Simple letter component - attached to each letter cube.
    /// Includes floating animation like the old FloatingLetter.
    /// </summary>
    public class SimpleLetter : MonoBehaviour
    {
        public char letter;
        public SimpleWordGame game;
        public Vector3 originalPosition;
        public int currentSocketIndex = -1;
        
        // Floating animation
        public bool enableFloating = true;
        public float floatAmplitude = 0.03f;
        public float floatSpeed = 1.5f;
        public float floatOffset = 0f;
        
        private XRGrabInteractable grab;
        private bool isHeld = false;
        
        private void Start()
        {
            grab = GetComponent<XRGrabInteractable>();
            if (grab != null)
            {
                grab.selectEntered.AddListener(OnGrabbed);
                grab.selectExited.AddListener(OnReleased);
            }
        }
        
        private void Update()
        {
            // Floating animation when not held and not in socket
            if (enableFloating && !isHeld && currentSocketIndex < 0)
            {
                float yOffset = Mathf.Sin((Time.time * floatSpeed) + floatOffset) * floatAmplitude;
                Vector3 targetPos = originalPosition + new Vector3(0, yOffset, 0);
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 3f);
            }
        }
        
        private void OnGrabbed(SelectEnterEventArgs args)
        {
            isHeld = true;
            
            // If was in a socket, notify game
            if (currentSocketIndex >= 0)
            {
                game.OnLetterExitedSocket(currentSocketIndex, letter);
                currentSocketIndex = -1;
            }
        }
        
        private void OnReleased(SelectExitEventArgs args)
        {
            isHeld = false;
            
            // Check if near a socket
            SimpleSocket[] sockets = FindObjectsByType<SimpleSocket>(FindObjectsSortMode.None);
            
            float snapDistance = 0.35f; // Increased snap distance for easier placement
            SimpleSocket nearestSocket = null;
            float nearestDist = float.MaxValue;
            
            foreach (var socket in sockets)
            {
                float dist = Vector3.Distance(transform.position, socket.transform.position);
                if (dist < snapDistance && dist < nearestDist)
                {
                    nearestSocket = socket;
                    nearestDist = dist;
                }
            }
            
            if (nearestSocket != null)
            {
                game.OnLetterEnteredSocket(nearestSocket.index, letter, gameObject);
                currentSocketIndex = nearestSocket.index;
                Debug.Log($"[SimpleLetter] '{letter}' snapped to socket {nearestSocket.index} (dist: {nearestDist:F2})");
                return;
            }
            
            // Not near any socket - return to original position
            Debug.Log($"[SimpleLetter] '{letter}' released but not near any socket");
            // (the Update loop will handle floating back)
        }
        
        private void OnDestroy()
        {
            if (grab != null)
            {
                grab.selectEntered.RemoveListener(OnGrabbed);
                grab.selectExited.RemoveListener(OnReleased);
            }
        }
    }
    
    /// <summary>
    /// Simple socket component - attached to each socket.
    /// Includes subtle floating animation.
    /// </summary>
    public class SimpleSocket : MonoBehaviour
    {
        public int index;
        public SimpleWordGame game;
        public Vector3 originalPosition;
        public float floatOffset;
        
        private void Start()
        {
            originalPosition = transform.position;
            floatOffset = index * 0.5f; // Offset each socket for wave effect
        }
        
        private void Update()
        {
            // Subtle floating animation for sockets
            float yOffset = Mathf.Sin((Time.time * 1f) + floatOffset) * 0.015f;
            transform.position = originalPosition + new Vector3(0, yOffset, 0);
        }
    }
}
