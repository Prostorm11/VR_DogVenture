using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.InputSystem;
using TMPro;
using VRProject.Core;

namespace VRProject.UI
{
    /// <summary>
    /// In-game side menu HUD that follows the player on the right side.
    /// Shows score, current word, hints, and quick actions.
    /// Press Menu button on controller (or Tab on keyboard) to toggle.
    /// </summary>
    public class InGameSideMenu : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private InputActionReference menuToggleAction; // Assign in Inspector
        [SerializeField] private Key toggleKey = Key.Tab;
        
        [Header("Position Settings")]
        [SerializeField] private float distanceFromPlayer = 0.5f;
        [SerializeField] private float rightAngle = 45f; // Angle to the right of forward view
        [SerializeField] private float heightOffset = -0.1f; // Relative to eye level
        [SerializeField] private float followSpeed = 5f;
        
        [Header("Panel Size (at default 1.6m height)")]
        [SerializeField] private float basePanelWidth = 0.24f;
        [SerializeField] private float basePanelHeight = 0.18f;
        [SerializeField] private float defaultUserHeight = 1.6f;
        
        [Header("Visibility")]
        [SerializeField] private bool startVisible = false; // Changed: menu starts hidden
        
        [Header("Colors")]
        [SerializeField] private Color backgroundColor = new Color(0.08f, 0.1f, 0.15f, 0.92f);
        [SerializeField] private Color borderColor = new Color(0.3f, 0.5f, 0.8f, 0.8f);
        [SerializeField] private Color titleColor = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private Color textColor = new Color(0.9f, 0.9f, 0.95f);
        [SerializeField] private Color scoreColor = new Color(0.4f, 1f, 0.4f);
        [SerializeField] private Color buttonColor = new Color(0.25f, 0.45f, 0.7f);
        [SerializeField] private Color buttonHoverColor = new Color(0.35f, 0.55f, 0.8f);
        
        private Transform player;
        private GameObject menuPanel;
        private bool isVisible = false;
        private float userHeight = 1.6f; // Default, will be detected
        private float heightScale = 1f; // Scale factor based on user height
        
        // Actual panel dimensions after scaling
        private float panelWidth;
        private float panelHeight;
        
        // UI Elements
        private TextMeshPro scoreLabel;
        private TextMeshPro wordsFoundLabel;
        private TextMeshPro currentWordLabel;
        private TextMeshPro hintsLabel;
        
        // Tracking game state
        private int displayedScore = 0;
        private int displayedWordsFound = 0;
        private string displayedCurrentWord = "";
        
        private void Start()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                player = cam.transform;
                // Detect user height from camera position
                userHeight = player.position.y;
                if (userHeight < 0.5f) userHeight = 1.6f; // Fallback if too low
                
                // Calculate scale factor
                heightScale = userHeight / defaultUserHeight;
                
                // Calculate actual panel dimensions based on height
                panelWidth = basePanelWidth * heightScale;
                panelHeight = basePanelHeight * heightScale;
                
                Debug.Log($"[InGameSideMenu] Detected user height: {userHeight:F2}m, Scale: {heightScale:F2}");
            }
            else
            {
                Debug.LogError("[InGameSideMenu] No main camera found!");
                return;
            }
            
            // Setup input action for controller button
            if (menuToggleAction != null && menuToggleAction.action != null)
            {
                menuToggleAction.action.Enable();
                menuToggleAction.action.performed += OnMenuTogglePressed;
            }
            
            // Only show at start if configured to do so
            if (startVisible)
            {
                Invoke(nameof(ShowMenu), 0.3f);
            }
            
            Debug.Log("[InGameSideMenu] Ready - Press Menu button or Tab to toggle");
        }

        private void OnDestroy()
        {
            // Cleanup input action
            if (menuToggleAction != null && menuToggleAction.action != null)
            {
                menuToggleAction.action.performed -= OnMenuTogglePressed;
            }
        }

        private void OnMenuTogglePressed(InputAction.CallbackContext context)
        {
            ToggleMenu();
        }

        public void ToggleMenu()
        {
            if (isVisible)
                HideMenu();
            else
                ShowMenu();
        }
        
        private void Update()
        {
            // Toggle visibility with keyboard (Tab key)
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                ToggleMenu();
            }
            
            // Update menu position to follow player
            if (isVisible && menuPanel != null && player != null)
            {
                UpdateMenuPosition();
            }
        }
        
        private void UpdateMenuPosition()
        {
            // Calculate position at an angle to the right of the player's view
            Vector3 forward = player.forward;
            forward.y = 0;
            forward.Normalize();
            
            // Rotate forward vector by rightAngle degrees to get menu direction
            Quaternion rotation = Quaternion.Euler(0, rightAngle, 0);
            Vector3 menuDirection = rotation * forward;
            
            // Position the menu - scale distance with height
            float scaledDistance = distanceFromPlayer * heightScale;
            float scaledHeightOffset = heightOffset * heightScale;
            
            Vector3 targetPos = player.position 
                + menuDirection * scaledDistance 
                + Vector3.up * scaledHeightOffset;
            
            // Smoothly move to target position
            menuPanel.transform.position = Vector3.Lerp(
                menuPanel.transform.position, 
                targetPos, 
                followSpeed * Time.deltaTime
            );
            
            // Always face the player (rotate to look at player)
            Vector3 lookDir = player.position - menuPanel.transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(-lookDir);
                menuPanel.transform.rotation = Quaternion.Slerp(
                    menuPanel.transform.rotation, 
                    targetRot, 
                    followSpeed * Time.deltaTime
                );
            }
        }
        
        public void ShowMenu()
        {
            if (menuPanel != null)
            {
                Destroy(menuPanel);
            }
            
            CreateMenu();
            isVisible = true;
            Debug.Log("[InGameSideMenu] Menu shown");
        }
        
        public void HideMenu()
        {
            if (menuPanel != null)
            {
                Destroy(menuPanel);
                menuPanel = null;
            }
            isVisible = false;
            Debug.Log("[InGameSideMenu] Menu hidden");
        }
        
        private void CreateMenu()
        {
            if (player == null) return;
            
            // Calculate initial position at angle to right
            Vector3 forward = player.forward;
            forward.y = 0;
            forward.Normalize();
            
            Quaternion rotation = Quaternion.Euler(0, rightAngle, 0);
            Vector3 menuDirection = rotation * forward;
            
            float scaledDistance = distanceFromPlayer * heightScale;
            float scaledHeightOffset = heightOffset * heightScale;
            
            Vector3 menuPos = player.position + menuDirection * scaledDistance + Vector3.up * scaledHeightOffset;
            
            // Create panel container
            menuPanel = new GameObject("InGameSideMenu");
            menuPanel.transform.position = menuPos;
            menuPanel.transform.localScale = Vector3.one; // Base scale, individual elements scaled
            
            // Create a nice border frame first
            CreateBorder();
            
            // Background panel
            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "MenuBackground";
            bg.transform.SetParent(menuPanel.transform);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localRotation = Quaternion.identity;
            bg.transform.localScale = new Vector3(panelWidth, panelHeight, 1f);
            Destroy(bg.GetComponent<Collider>());
            
            Material bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (bgMat.shader == null) bgMat = new Material(Shader.Find("Unlit/Color"));
            bgMat.color = backgroundColor;
            bg.GetComponent<Renderer>().material = bgMat;
            
            // Calculate positions based on panel size
            float topY = panelHeight * 0.38f;
            float lineSpacing = panelHeight * 0.14f;
            float textDepth = -0.005f;
            
            // Scale font sizes based on height (base sizes designed for 1.6m user)
            float fontScale = heightScale;
            
            // Title with underline effect
            CreateText("GAME INFO", new Vector3(0, topY, textDepth), 3f * fontScale, titleColor, true);
            CreateText("----------", new Vector3(0, topY - lineSpacing * 0.5f, textDepth), 1.8f * fontScale, borderColor, false);
            
            // Current Word
            currentWordLabel = CreateText("Word: ---", new Vector3(0, topY - lineSpacing * 1.2f, textDepth), 2.4f * fontScale, Color.cyan, false);
            
            // Score (prominent)
            scoreLabel = CreateText("Score: 0", new Vector3(0, topY - lineSpacing * 2.0f, textDepth), 2.8f * fontScale, scoreColor, true);
            
            // Words Found
            wordsFoundLabel = CreateText("Words: 0", new Vector3(0, topY - lineSpacing * 2.8f, textDepth), 2.2f * fontScale, textColor, false);
            
            // Divider
            CreateText("--------", new Vector3(0, topY - lineSpacing * 3.4f, textDepth), 1.6f * fontScale, new Color(0.4f, 0.5f, 0.6f), false);
            
            // Buttons at bottom
            float btnY = topY - lineSpacing * 4.2f;
            float btnWidth = panelWidth * 0.38f;
            CreateButton("HINT", new Vector3(-panelWidth * 0.14f, btnY, textDepth), buttonColor, OnHintPressed, btnWidth);
            CreateButton("PAUSE", new Vector3(panelWidth * 0.14f, btnY, textDepth), new Color(0.6f, 0.35f, 0.2f), OnPausePressed, btnWidth);
            
            // Face the player
            Vector3 lookDir = player.position - menuPanel.transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                menuPanel.transform.rotation = Quaternion.LookRotation(-lookDir);
            }
        }
        
        private void CreateBorder()
        {
            float borderThickness = 0.003f * heightScale;
            float depth = 0.001f;
            
            // Create border frame around the panel
            CreateBorderEdge("Top", new Vector3(0, panelHeight/2, depth), new Vector3(panelWidth + borderThickness*2, borderThickness, 0.001f));
            CreateBorderEdge("Bottom", new Vector3(0, -panelHeight/2, depth), new Vector3(panelWidth + borderThickness*2, borderThickness, 0.001f));
            CreateBorderEdge("Left", new Vector3(-panelWidth/2, 0, depth), new Vector3(borderThickness, panelHeight, 0.001f));
            CreateBorderEdge("Right", new Vector3(panelWidth/2, 0, depth), new Vector3(borderThickness, panelHeight, 0.001f));
        }
        
        private void CreateBorderEdge(string name, Vector3 localPos, Vector3 scale)
        {
            GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Quad);
            edge.name = $"Border_{name}";
            edge.transform.SetParent(menuPanel.transform);
            edge.transform.localPosition = localPos;
            edge.transform.localRotation = Quaternion.identity;
            edge.transform.localScale = scale;
            Destroy(edge.GetComponent<Collider>());
            
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (mat.shader == null) mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = borderColor;
            edge.GetComponent<Renderer>().material = mat;
        }
        
        private TextMeshPro CreateText(string text, Vector3 localPos, float fontSize, Color color, bool bold)
        {
            GameObject textObj = new GameObject($"Text_{text.Replace(" ", "").Substring(0, Mathf.Min(8, text.Replace(" ", "").Length))}");
            textObj.transform.SetParent(menuPanel.transform);
            textObj.transform.localPosition = localPos;
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = Vector3.one * 0.01f; // Scale down for proper TMP sizing
            
            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            if (bold) tmp.fontStyle = FontStyles.Bold;
            
            // Set rect size for proper text display
            tmp.rectTransform.sizeDelta = new Vector2(panelWidth * 100f, 10f);
            
            return tmp;
        }
        
        private void CreateButton(string text, Vector3 localPos, Color color, System.Action onClick, float width = 0.1f)
        {
            // Scale button height with user height
            float btnHeight = 0.018f * heightScale;
            float btnDepth = 0.006f * heightScale;
            
            GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            btn.name = $"Button_{text}";
            btn.transform.SetParent(menuPanel.transform);
            btn.transform.localPosition = localPos;
            btn.transform.localRotation = Quaternion.identity;
            btn.transform.localScale = new Vector3(width, btnHeight, btnDepth);
            
            Material btnMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (btnMat.shader == null) btnMat = new Material(Shader.Find("Standard"));
            btnMat.color = color;
            btn.GetComponent<Renderer>().material = btnMat;
            
            // Button text - positioned in front of button
            GameObject textObj = new GameObject("ButtonText");
            textObj.transform.SetParent(btn.transform);
            textObj.transform.localPosition = new Vector3(0, 0, -0.55f);
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = new Vector3(1f / width * 0.01f, 1f / btnHeight * 0.01f, 1f);
            
            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 2.2f * heightScale;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.rectTransform.sizeDelta = new Vector2(width * 100f, btnHeight * 100f);
            
            // Make interactable for VR
            XRSimpleInteractable interactable = btn.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener((args) => onClick?.Invoke());
        }
        
        #region Button Actions
        
        private void OnHintPressed()
        {
            if (VRProject.WordPuzzle.HintSystem.Instance != null)
            {
                VRProject.WordPuzzle.HintSystem.Instance.RequestHint();
                Debug.Log("[InGameSideMenu] Hint requested");
            }
            else
            {
                Debug.Log("[InGameSideMenu] HintSystem not found");
            }
        }
        
        private void OnPausePressed()
        {
            // Find GameMenuUI and show it
            GameMenuUI gameMenu = FindAnyObjectByType<GameMenuUI>();
            if (gameMenu != null)
            {
                gameMenu.ShowMenu();
            }
            else
            {
                Debug.Log("[InGameSideMenu] GameMenuUI not found - pausing manually");
                Time.timeScale = 0f;
            }
        }
        
        #endregion
        
        #region Public Update Methods
        
        /// <summary>
        /// Update the score display
        /// </summary>
        public void UpdateScore(int newScore)
        {
            displayedScore = newScore;
            if (scoreLabel != null)
            {
                scoreLabel.text = $"Score: {newScore}";
            }
        }
        
        /// <summary>
        /// Update the words found count
        /// </summary>
        public void UpdateWordsFound(int count)
        {
            displayedWordsFound = count;
            if (wordsFoundLabel != null)
            {
                wordsFoundLabel.text = $"Words: {count}";
            }
        }
        
        /// <summary>
        /// Update the current base word display
        /// </summary>
        public void UpdateCurrentWord(string word)
        {
            displayedCurrentWord = word;
            if (currentWordLabel != null)
            {
                currentWordLabel.text = $"Word: {word}";
            }
        }
        
        /// <summary>
        /// Update hints remaining
        /// </summary>
        public void UpdateHints(int remaining)
        {
            if (hintsLabel != null)
            {
                hintsLabel.text = $"Hints: {remaining}";
            }
        }
        
        #endregion
    }
}
