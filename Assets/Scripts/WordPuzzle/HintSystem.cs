using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace VRDogVenture.WordPuzzle
{
    /// <summary>
    /// Provides hints to the player about valid words they can form.
    /// </summary>
    public class HintSystem : MonoBehaviour
    {
        public static HintSystem Instance { get; private set; }
        
        [Header("Hint Settings")]
        [SerializeField] private float hintCooldown = 10f;
        [SerializeField] private int maxHintsPerWord = 3;
        [SerializeField] private bool showPartialHints = true;
        
        [Header("Visual Settings")]
        [SerializeField] private Color hintColor = Color.cyan;
        [SerializeField] private float hintDisplayTime = 5f;
        [SerializeField] private float defaultUserHeight = 1.6f;
        
        private float lastHintTime = -100f;
        private int hintsUsedThisWord = 0;
        private string currentBaseWord;
        private HashSet<string> wordsAlreadyHinted = new HashSet<string>();
        
        private GameObject hintPopup;
        private Transform player;
        private float userHeightScale = 1f;
        
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
            Camera cam = Camera.main;
            if (cam != null)
            {
                player = cam.transform;
                // Calculate height scale for proper sizing
                float userHeight = player.position.y;
                if (userHeight > 0.5f)
                {
                    userHeightScale = userHeight / defaultUserHeight;
                }
            }
            Debug.Log($"[HintSystem] Ready (height scale: {userHeightScale:F2})");
        }
        
        /// <summary>
        /// Set the current base word for hints
        /// </summary>
        public void SetBaseWord(string baseWord)
        {
            currentBaseWord = baseWord.ToUpper();
            hintsUsedThisWord = 0;
            wordsAlreadyHinted.Clear();
            Debug.Log($"[HintSystem] Base word set to: {currentBaseWord}");
        }
        
        /// <summary>
        /// Request a hint for the current word
        /// </summary>
        public string RequestHint(int targetLength = 0)
        {
            // Check cooldown
            if (Time.time - lastHintTime < hintCooldown)
            {
                float remaining = hintCooldown - (Time.time - lastHintTime);
                ShowHintMessage($"Wait {remaining:F1}s for next hint", Color.yellow);
                return null;
            }
            
            // Check max hints
            if (hintsUsedThisWord >= maxHintsPerWord)
            {
                ShowHintMessage("No more hints for this word!", Color.red);
                return null;
            }
            
            // Get a valid word to hint
            string hint = GetNextHint(targetLength);
            
            if (string.IsNullOrEmpty(hint))
            {
                ShowHintMessage("No hints available", Color.yellow);
                return null;
            }
            
            lastHintTime = Time.time;
            hintsUsedThisWord++;
            wordsAlreadyHinted.Add(hint);
            
            // Show the hint
            string displayHint = showPartialHints ? GetPartialHint(hint) : hint;
            ShowHintMessage($"Try: {displayHint}", hintColor);
            
            Debug.Log($"[HintSystem] Hint given: {hint} (displayed as: {displayHint})");
            return hint;
        }
        
        private string GetNextHint(int targetLength)
        {
            if (WordValidator.Instance == null || string.IsNullOrEmpty(currentBaseWord))
                return null;
            
            List<string> candidates;
            
            if (targetLength > 0)
            {
                candidates = WordValidator.Instance.GetValidWordsOfLength(currentBaseWord, targetLength);
            }
            else
            {
                // Get all valid words
                string[] allWords = WordValidator.Instance.GetValidWords(currentBaseWord);
                candidates = new List<string>(allWords);
            }
            
            // Filter out already hinted words and single letters
            candidates.RemoveAll(w => wordsAlreadyHinted.Contains(w) || w.Length < 2);
            
            if (candidates.Count == 0)
                return null;
            
            // Sort by length (prefer shorter words for easier hints)
            candidates.Sort((a, b) => a.Length.CompareTo(b.Length));
            
            // Return a random word from the shorter ones
            int maxIndex = Mathf.Min(3, candidates.Count);
            return candidates[Random.Range(0, maxIndex)];
        }
        
        private string GetPartialHint(string word)
        {
            if (word.Length <= 2)
                return word;
            
            // Show first and last letter, hide middle
            char[] hint = new char[word.Length];
            hint[0] = word[0];
            hint[word.Length - 1] = word[word.Length - 1];
            
            for (int i = 1; i < word.Length - 1; i++)
            {
                hint[i] = '_';
            }
            
            return new string(hint);
        }
        
        private void ShowHintMessage(string message, Color color)
        {
            // Clean up old popup
            if (hintPopup != null)
            {
                Destroy(hintPopup);
            }
            
            if (player == null)
            {
                Camera cam = Camera.main;
                if (cam != null) player = cam.transform;
            }
            
            if (player == null) return;
            
            // Create popup container
            hintPopup = new GameObject("HintPopup");
            Vector3 forward = player.forward;
            forward.y = 0;
            forward.Normalize();
            
            // Position in front of player, slightly above eye level
            float distance = 1.2f * userHeightScale;
            float heightOffset = 0.3f * userHeightScale;
            hintPopup.transform.position = player.position + forward * distance + Vector3.up * heightOffset;
            
            // Create background panel
            float panelWidth = 0.4f * userHeightScale;
            float panelHeight = 0.12f * userHeightScale;
            
            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "HintBackground";
            bg.transform.SetParent(hintPopup.transform);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localRotation = Quaternion.identity;
            bg.transform.localScale = new Vector3(panelWidth, panelHeight, 1f);
            Object.Destroy(bg.GetComponent<Collider>());
            
            // Background material with transparency
            Material bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (bgMat.shader == null) bgMat = new Material(Shader.Find("Unlit/Color"));
            bgMat.color = new Color(0.05f, 0.08f, 0.15f, 0.9f);
            bg.GetComponent<Renderer>().material = bgMat;
            
            // Create border frame
            CreateHintBorder(panelWidth, panelHeight, color);
            
            // Add icon/decoration based on message type (using ASCII-compatible symbols)
            string icon = "*";
            if (message.Contains("Wait")) icon = ">";
            else if (message.Contains("No more") || message.Contains("No hints")) icon = "X";
            
            // Create main text
            GameObject textObj = new GameObject("HintText");
            textObj.transform.SetParent(hintPopup.transform);
            textObj.transform.localPosition = new Vector3(0, 0, -0.005f);
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = Vector3.one * 0.01f;
            
            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = $"{icon} {message}";
            tmp.fontSize = 4f * userHeightScale;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.rectTransform.sizeDelta = new Vector2(panelWidth * 100f, panelHeight * 100f);
            
            // Add subtle glow effect behind text
            GameObject glowObj = new GameObject("HintGlow");
            glowObj.transform.SetParent(hintPopup.transform);
            glowObj.transform.localPosition = new Vector3(0, 0, -0.003f);
            glowObj.transform.localRotation = Quaternion.identity;
            glowObj.transform.localScale = Vector3.one * 0.01f;
            
            TextMeshPro glowTmp = glowObj.AddComponent<TextMeshPro>();
            glowTmp.text = $"{icon} {message}";
            glowTmp.fontSize = 4.2f * userHeightScale;
            glowTmp.alignment = TextAlignmentOptions.Center;
            glowTmp.color = new Color(color.r, color.g, color.b, 0.3f);
            glowTmp.fontStyle = FontStyles.Bold;
            glowTmp.textWrappingMode = TextWrappingModes.NoWrap;
            glowTmp.rectTransform.sizeDelta = new Vector2(panelWidth * 100f, panelHeight * 100f);
            
            // Face camera
            Vector3 lookDir = player.position - hintPopup.transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                hintPopup.transform.rotation = Quaternion.LookRotation(-lookDir);
            }
            
            // Add fade-in animation
            StartCoroutine(AnimateHintPopup());
            
            // Auto destroy
            Destroy(hintPopup, hintDisplayTime);
        }
        
        private void CreateHintBorder(float width, float height, Color color)
        {
            float borderThickness = 0.004f * userHeightScale;
            float depth = 0.001f;
            
            Color borderColor = new Color(color.r, color.g, color.b, 0.8f);
            
            // Top border
            CreateBorderEdge("Top", new Vector3(0, height/2, depth), new Vector3(width + borderThickness*2, borderThickness, 0.001f), borderColor);
            // Bottom border
            CreateBorderEdge("Bottom", new Vector3(0, -height/2, depth), new Vector3(width + borderThickness*2, borderThickness, 0.001f), borderColor);
            // Left border
            CreateBorderEdge("Left", new Vector3(-width/2, 0, depth), new Vector3(borderThickness, height, 0.001f), borderColor);
            // Right border
            CreateBorderEdge("Right", new Vector3(width/2, 0, depth), new Vector3(borderThickness, height, 0.001f), borderColor);
        }
        
        private void CreateBorderEdge(string name, Vector3 localPos, Vector3 scale, Color color)
        {
            GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Quad);
            edge.name = $"Border_{name}";
            edge.transform.SetParent(hintPopup.transform);
            edge.transform.localPosition = localPos;
            edge.transform.localRotation = Quaternion.identity;
            edge.transform.localScale = scale;
            Object.Destroy(edge.GetComponent<Collider>());
            
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (mat.shader == null) mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = color;
            edge.GetComponent<Renderer>().material = mat;
        }
        
        private IEnumerator AnimateHintPopup()
        {
            if (hintPopup == null) yield break;
            
            // Simple scale-in animation
            Vector3 targetScale = hintPopup.transform.localScale;
            hintPopup.transform.localScale = targetScale * 0.5f;
            
            float elapsed = 0f;
            float duration = 0.2f;
            
            while (elapsed < duration && hintPopup != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = 1f - (1f - t) * (1f - t); // Ease out quad
                hintPopup.transform.localScale = Vector3.Lerp(targetScale * 0.5f, targetScale, t);
                yield return null;
            }
            
            if (hintPopup != null)
            {
                hintPopup.transform.localScale = targetScale;
            }
        }
        
        /// <summary>
        /// Check if hints are available
        /// </summary>
        public bool CanGetHint()
        {
            if (Time.time - lastHintTime < hintCooldown)
                return false;
            if (hintsUsedThisWord >= maxHintsPerWord)
                return false;
            return true;
        }
        
        /// <summary>
        /// Get remaining cooldown time
        /// </summary>
        public float GetCooldownRemaining()
        {
            float remaining = hintCooldown - (Time.time - lastHintTime);
            return Mathf.Max(0, remaining);
        }
        
        /// <summary>
        /// Get hints remaining for current word
        /// </summary>
        public int GetHintsRemaining()
        {
            return Mathf.Max(0, maxHintsPerWord - hintsUsedThisWord);
        }
    }
}
