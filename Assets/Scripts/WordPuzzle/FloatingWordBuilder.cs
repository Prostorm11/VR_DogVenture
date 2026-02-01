using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using VRDogVenture.Core;
using VRDogVenture.Events;

namespace VRDogVenture.WordPuzzle
{
    /// <summary>
    /// Manages floating letters and the answer building zone.
    /// Letters float in the "letter pool" area and can be dragged to the "answer zone".
    /// No physical slots - just floating snap positions.
    /// </summary>
    public class FloatingWordBuilder : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GameObject floatingLetterPrefab;

        [Header("Letter Pool Settings (Where scrambled letters float)")]
        [SerializeField] private Transform letterPoolCenter;
        [SerializeField] private float poolSpacing = 0.15f; // Increased for easier grabbing
        [SerializeField] private float poolHeight = 1.2f;
        [SerializeField] private float poolDistance = 0.6f; // Closer to player

        [Header("Answer Zone Settings (Where player builds word)")]
        [SerializeField] private Transform answerZoneCenter;
        [SerializeField] private float answerSpacing = 0.12f; // Space between answer slots
        [SerializeField] private float answerHeight = 1.0f;
        [SerializeField] private float answerDistance = 0.4f; // Closer for easier placement
        [SerializeField] private float snapDistance = 0.5f; // VERY large snap area for VR
        
        // Force minimum snap distance in case serialized value is too small
        private float ActualSnapDistance => Mathf.Max(snapDistance, 0.5f);

        [Header("Answer Zone Visuals")]
        [SerializeField] private bool showAnswerGuides = true;
        [SerializeField] private GameObject answerSlotGuidePrefab; // Optional: visual guide cubes
        [SerializeField] private Color guideColor = new Color(1f, 1f, 1f, 0.3f);

        [Header("Base Word Display")]
        [SerializeField] private TextMeshPro baseWordDisplay;
        [SerializeField] private Vector3 baseWordOffset = new Vector3(0, 0.4f, 0);

        [Header("Word Lists")]
        [SerializeField] private List<string> easyWords = new List<string>
        {
            "STAR", "RATS", "ARTS", "TARS",  // Anagrams of STAR
            "STOP", "POTS", "TOPS", "SPOT",  // Anagrams of STOP
            "TEAM", "MEAT", "MATE", "TAME",  // Anagrams of TEAM
            "LOOP", "POOL", "POLO",          // Anagrams of LOOP
            "SLOW", "OWLS", "LOWS"           // Anagrams of SLOW
        };
        [SerializeField] private List<string> mediumWords = new List<string>
        {
            "HEART", "EARTH", "HATER",       // Anagrams
            "MEATS", "STEAM", "TEAMS", "MATES", // 5-letter anagrams
            "NOTES", "STONE", "TONES", "ONSET", // More 5-letter
            "ANGEL", "GLEAN", "ANGLE"        // Anagrams
        };
        [SerializeField] private List<string> hardWords = new List<string>
        {
            "STREAM", "MASTER", "HEARTS",    // 6-letter words
            "LISTEN", "SILENT", "TINSEL",    // Anagrams  
            "GARDEN", "DANGER", "GANDER",    // More 6-letter
            "RESCUE", "SECURE", "RECUSE"     // Complex
        };

        // State
        private List<FloatingLetter> allLetters = new List<FloatingLetter>();
        private FloatingLetter[] answerSlots; // Letters in answer zone (by position)
        private List<GameObject> answerGuides = new List<GameObject>();
        private string currentBaseWord = "";
        private int maxAnswerSlots = 8;
        private int currentWordIndex = 0;

        public string CurrentBaseWord => currentBaseWord;

        private void Awake()
        {
            // Initialize arrays early so they're ready before Start
            answerSlots = new FloatingLetter[maxAnswerSlots];
            allLetters = new List<FloatingLetter>();
        }

        private void Start()
        {
            // Ensure array exists (in case Awake didn't run)
            if (answerSlots == null)
                answerSlots = new FloatingLetter[maxAnswerSlots];

            // Set default positions if not assigned
            if (letterPoolCenter == null)
            {
                GameObject poolObj = new GameObject("LetterPoolCenter");
                poolObj.transform.SetParent(transform);
                poolObj.transform.localPosition = new Vector3(0, poolHeight, poolDistance);
                letterPoolCenter = poolObj.transform;
            }

            if (answerZoneCenter == null)
            {
                GameObject answerObj = new GameObject("AnswerZoneCenter");
                answerObj.transform.SetParent(transform);
                answerObj.transform.localPosition = new Vector3(0, answerHeight, answerDistance);
                answerZoneCenter = answerObj.transform;
            }

            // Create answer zone guides
            if (showAnswerGuides)
            {
                CreateAnswerGuides();
            }
        }

        /// <summary>
        /// Spawn floating letters for a new word.
        /// </summary>
        public void SpawnWord(string word)
        {
            // Ensure initialization (in case called before Start)
            EnsureInitialized();
            
            ClearAllLetters();
            
            currentBaseWord = word.ToUpper();
            
            // Update base word display - show the original word clearly above
            if (baseWordDisplay == null && letterPoolCenter != null)
            {
                CreateBaseWordDisplay();
            }
            
            if (baseWordDisplay != null)
            {
                baseWordDisplay.text = $"BASE WORD: {currentBaseWord}";
            }

            // Shuffle letters for the pool
            char[] letters = currentBaseWord.ToCharArray();
            ShuffleArray(letters);

            // Calculate positions for letter pool - arrange in a neat row
            float totalWidth = (letters.Length - 1) * poolSpacing;
            Vector3 centerPos = letterPoolCenter != null ? letterPoolCenter.position : transform.position + new Vector3(0, poolHeight, poolDistance);
            Vector3 startPos = centerPos - new Vector3(totalWidth / 2f, 0, 0);

            // Spawn each letter in a neat row (no random offset for cleaner look)
            for (int i = 0; i < letters.Length; i++)
            {
                Vector3 position = startPos + new Vector3(i * poolSpacing, 0, 0);
                SpawnLetter(letters[i], position);
            }

            // Update answer guides
            UpdateAnswerGuides(currentBaseWord.Length);

            Debug.Log($"Spawned {letters.Length} floating letters for: {currentBaseWord}");
            GameEvents.TriggerNewBaseWord(currentBaseWord);
        }

        /// <summary>
        /// Ensure all required objects are initialized.
        /// </summary>
        private void EnsureInitialized()
        {
            if (answerSlots == null)
                answerSlots = new FloatingLetter[maxAnswerSlots];
            
            if (allLetters == null)
                allLetters = new List<FloatingLetter>();

            if (letterPoolCenter == null)
            {
                GameObject poolObj = new GameObject("LetterPoolCenter");
                poolObj.transform.SetParent(transform);
                poolObj.transform.localPosition = new Vector3(0, poolHeight, poolDistance);
                letterPoolCenter = poolObj.transform;
            }

            if (answerZoneCenter == null)
            {
                GameObject answerObj = new GameObject("AnswerZoneCenter");
                answerObj.transform.SetParent(transform);
                answerObj.transform.localPosition = new Vector3(0, answerHeight, answerDistance);
                answerZoneCenter = answerObj.transform;
            }
        }

        private void SpawnLetter(char c, Vector3 position)
        {
            if (floatingLetterPrefab == null)
            {
                Debug.LogError("FloatingLetter prefab not assigned!");
                return;
            }

            // Face the player (camera) - rotate 180 degrees on Y so letters face the right way
            Camera cam = Camera.main;
            Quaternion rotation = Quaternion.identity;
            if (cam != null)
            {
                Vector3 lookDir = cam.transform.position - position;
                lookDir.y = 0; // Keep upright
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    rotation = Quaternion.LookRotation(lookDir);
                }
            }
            
            GameObject letterObj = Instantiate(floatingLetterPrefab, position, rotation, transform);
            FloatingLetter letter = letterObj.GetComponent<FloatingLetter>();
            
            if (letter != null)
            {
                letter.Initialize(c);
                letter.SetFloatPosition(position);
                allLetters.Add(letter);
            }
        }

        /// <summary>
        /// Get the next base word based on difficulty level.
        /// Each word is only shown once before moving to the next.
        /// </summary>
        public string GetNextWord(int level)
        {
            List<string> wordList;
            
            if (level <= 2)
                wordList = easyWords;
            else if (level <= 4)
                wordList = mediumWords;
            else
                wordList = hardWords;

            if (wordList.Count == 0) return "TEST";

            // Get a different word each time (cycle through list)
            string word = wordList[currentWordIndex % wordList.Count];
            currentWordIndex++;
            
            Debug.Log($"[WordBuilder] GetNextWord called - Level: {level}, Index: {currentWordIndex}, Word: {word}");
            return word;
        }

        /// <summary>
        /// Try to snap a letter to the answer zone.
        /// Returns true if snapped successfully.
        /// </summary>
        public bool TrySnapLetterToAnswer(FloatingLetter letter)
        {
            // Find the nearest empty answer slot
            int nearestSlot = -1;
            float actualSnap = ActualSnapDistance; // Use forced minimum
            float nearestDistance = actualSnap;

            Vector3 answerCenter = answerZoneCenter != null ? answerZoneCenter.position : transform.position;
            Debug.Log($"[WordBuilder] Checking snap for '{letter.Letter}' at {letter.transform.position}. Answer zone center: {answerCenter}, snap distance: {actualSnap}");

            for (int i = 0; i < maxAnswerSlots; i++)
            {
                if (answerSlots[i] != null) continue; // Slot occupied

                Vector3 slotPos = GetAnswerSlotPosition(i);
                float distance = Vector3.Distance(letter.transform.position, slotPos);

                Debug.Log($"  Slot {i}: pos={slotPos}, distance={distance:F3}");

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestSlot = i;
                }
            }

            if (nearestSlot >= 0)
            {
                // Snap to slot
                Vector3 snapPos = GetAnswerSlotPosition(nearestSlot);
                letter.SnapToPosition(snapPos, nearestSlot);
                answerSlots[nearestSlot] = letter;
                
                Debug.Log($"[WordBuilder] Snapped '{letter.Letter}' to slot {nearestSlot}!");
                GameEvents.TriggerLetterPlaced(letter.Letter);
                UpdateAnswerGuideVisuals();
                return true;
            }

            Debug.Log($"[WordBuilder] No slot close enough for '{letter.Letter}'");
            return false;
        }

        /// <summary>
        /// Remove a letter from the answer zone.
        /// </summary>
        public void RemoveLetterFromAnswer(FloatingLetter letter)
        {
            for (int i = 0; i < answerSlots.Length; i++)
            {
                if (answerSlots[i] == letter)
                {
                    answerSlots[i] = null;
                    GameEvents.TriggerLetterRemoved(letter.Letter);
                    break;
                }
            }
            UpdateAnswerGuideVisuals();
        }

        /// <summary>
        /// Get the current word formed in the answer zone.
        /// </summary>
        public string GetCurrentAnswer()
        {
            string answer = "";
            int filledSlots = 0;
            
            // Read letters from left to right (occupied slots only)
            for (int i = 0; i < answerSlots.Length; i++)
            {
                if (answerSlots[i] != null)
                {
                    answer += answerSlots[i].Letter;
                    filledSlots++;
                }
            }

            Debug.Log($"[WordBuilder] GetCurrentAnswer: '{answer}' ({filledSlots} slots filled of {maxAnswerSlots})");
            return answer;
        }

        /// <summary>
        /// Submit the current answer for validation.
        /// </summary>
        public void SubmitAnswer()
        {
            string answer = GetCurrentAnswer();
            
            if (string.IsNullOrEmpty(answer))
            {
                Debug.Log("No answer to submit!");
                return;
            }

            Debug.Log($"Submitting answer: {answer}");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SubmitWord(answer);
            }
        }

        /// <summary>
        /// Show visual feedback on answer letters.
        /// </summary>
        public void ShowAnswerFeedback(bool correct)
        {
            foreach (var letter in answerSlots)
            {
                if (letter != null)
                {
                    if (correct)
                        letter.ShowCorrect();
                    else
                        letter.ShowIncorrect();
                }
            }
        }

        /// <summary>
        /// Clear the answer zone and return letters to pool.
        /// </summary>
        public void ClearAnswer()
        {
            for (int i = 0; i < answerSlots.Length; i++)
            {
                if (answerSlots[i] != null)
                {
                    // Return letter to a random pool position
                    Vector3 poolPos = GetRandomPoolPosition();
                    answerSlots[i].ReturnToPool(poolPos);
                    answerSlots[i] = null;
                }
            }
            UpdateAnswerGuideVisuals();
        }

        /// <summary>
        /// Clear all letters completely.
        /// </summary>
        public void ClearAllLetters()
        {
            // Ensure list is initialized
            if (allLetters == null)
                allLetters = new List<FloatingLetter>();

            foreach (var letter in allLetters)
            {
                if (letter != null)
                {
                    Destroy(letter.gameObject);
                }
            }
            allLetters.Clear();
            
            // Ensure array is initialized
            if (answerSlots == null)
                answerSlots = new FloatingLetter[maxAnswerSlots];

            for (int i = 0; i < answerSlots.Length; i++)
            {
                answerSlots[i] = null;
            }
        }

        #region Position Calculations

        private Vector3 GetAnswerSlotPosition(int index)
        {
            float totalWidth = (maxAnswerSlots - 1) * answerSpacing;
            Vector3 startPos = answerZoneCenter.position - new Vector3(totalWidth / 2f, 0, 0);
            return startPos + new Vector3(index * answerSpacing, 0, 0);
        }

        private Vector3 GetRandomPoolPosition()
        {
            float totalWidth = (currentBaseWord.Length - 1) * poolSpacing;
            float x = Random.Range(-totalWidth / 2f, totalWidth / 2f);
            float y = Random.Range(-0.05f, 0.05f);
            float z = Random.Range(-0.05f, 0.05f);
            return letterPoolCenter.position + new Vector3(x, y, z);
        }

        #endregion

        #region Answer Guides

        private void CreateAnswerGuides()
        {
            // Create visual guide objects for answer slots
            for (int i = 0; i < maxAnswerSlots; i++)
            {
                GameObject guide;
                
                if (answerSlotGuidePrefab != null)
                {
                    guide = Instantiate(answerSlotGuidePrefab, GetAnswerSlotPosition(i), Quaternion.identity, transform);
                }
                else
                {
                    // Create visible guide cube
                    guide = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    guide.transform.SetParent(transform);
                    guide.transform.position = GetAnswerSlotPosition(i);
                    guide.transform.localScale = new Vector3(0.1f, 0.1f, 0.02f);
                    
                    // Make it visible with unlit material
                    Renderer rend = guide.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                        if (mat.shader == null)
                            mat = new Material(Shader.Find("Unlit/Color"));
                        mat.color = new Color(0.3f, 0.6f, 0.9f, 0.5f); // Light blue, visible
                        rend.material = mat;
                    }

                    // Remove collider so it doesn't interfere
                    Collider col = guide.GetComponent<Collider>();
                    if (col != null) Destroy(col);
                }

                guide.name = $"AnswerGuide_{i}";
                guide.SetActive(false);
                answerGuides.Add(guide);
            }
            
            // Create "ANSWER ZONE" label
            CreateAnswerZoneLabel();
        }
        
        private void CreateAnswerZoneLabel()
        {
            GameObject labelObj = new GameObject("AnswerZoneLabel");
            labelObj.transform.SetParent(transform);
            
            // Create background panel for better visibility
            GameObject bgPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bgPanel.name = "AnswerZoneLabelBg";
            bgPanel.transform.SetParent(labelObj.transform);
            bgPanel.transform.localScale = new Vector3(0.4f, 0.06f, 0.01f);
            bgPanel.transform.localPosition = new Vector3(0, 0, 0.01f);
            
            Renderer bgRend = bgPanel.GetComponent<Renderer>();
            if (bgRend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                if (mat.shader == null)
                    mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = new Color(0.1f, 0.2f, 0.3f, 0.9f);
                bgRend.material = mat;
            }
            Destroy(bgPanel.GetComponent<Collider>());
            
            TextMeshPro label = labelObj.AddComponent<TextMeshPro>();
            label.text = "[ PLACE LETTERS HERE ]";
            label.fontSize = 0.1f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.3f, 0.9f, 1f);
            label.fontStyle = FontStyles.Bold;
            
            if (answerZoneCenter != null)
            {
                labelObj.transform.position = answerZoneCenter.position + new Vector3(0, 0.15f, 0);
                labelObj.transform.rotation = Quaternion.identity; // Face forward
            }
        }

        private void CreateBaseWordDisplay()
        {
            // Create a visible panel for the base word above the letter pool
            GameObject displayObj = new GameObject("BaseWordDisplay");
            displayObj.transform.SetParent(transform);
            
            // Background panel
            GameObject bgPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bgPanel.name = "BaseWordBg";
            bgPanel.transform.SetParent(displayObj.transform);
            bgPanel.transform.localScale = new Vector3(0.5f, 0.1f, 0.01f);
            bgPanel.transform.localPosition = new Vector3(0, 0, 0.01f);
            
            Renderer bgRend = bgPanel.GetComponent<Renderer>();
            if (bgRend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                if (mat.shader == null)
                    mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = new Color(0.2f, 0.15f, 0.1f);
                bgRend.material = mat;
            }
            Destroy(bgPanel.GetComponent<Collider>());
            
            // Text
            baseWordDisplay = displayObj.AddComponent<TextMeshPro>();
            baseWordDisplay.fontSize = 0.12f;
            baseWordDisplay.alignment = TextAlignmentOptions.Center;
            baseWordDisplay.color = Color.yellow;
            baseWordDisplay.fontStyle = FontStyles.Bold;
            
            if (letterPoolCenter != null)
            {
                displayObj.transform.position = letterPoolCenter.position + baseWordOffset;
            }
        }

        private void UpdateAnswerGuides(int activeCount)
        {
            // Always show the max slots so player knows where to place letters
            int slotsToShow = Mathf.Max(activeCount, maxAnswerSlots);
            
            for (int i = 0; i < answerGuides.Count; i++)
            {
                if (answerGuides[i] != null)
                {
                    // Show all guides, but highlight active ones
                    answerGuides[i].SetActive(true);
                    answerGuides[i].transform.position = GetAnswerSlotPosition(i);
                    
                    // Make inactive slots dimmer
                    Renderer rend = answerGuides[i].GetComponent<Renderer>();
                    if (rend != null)
                    {
                        if (i < activeCount)
                        {
                            // Active slot - bright and visible
                            rend.material.color = new Color(0.3f, 0.8f, 1f, 0.6f);
                        }
                        else
                        {
                            // Inactive slot - very dim but still visible
                            rend.material.color = new Color(0.2f, 0.3f, 0.4f, 0.2f);
                        }
                    }
                }
            }
            UpdateAnswerGuideVisuals();
        }

        private void UpdateAnswerGuideVisuals()
        {
            // Dim guides that have letters in them
            for (int i = 0; i < answerGuides.Count; i++)
            {
                if (answerGuides[i] != null && answerGuides[i].activeSelf)
                {
                    Renderer rend = answerGuides[i].GetComponent<Renderer>();
                    if (rend != null)
                    {
                        Color color = answerSlots[i] != null 
                            ? new Color(guideColor.r, guideColor.g, guideColor.b, 0.1f) 
                            : guideColor;
                        rend.material.color = color;
                    }
                }
            }
        }

        #endregion

        #region Utilities

        private void ShuffleArray<T>(T[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
        }

        #endregion

        private void OnDestroy()
        {
            ClearAllLetters();
        }
    }
}
