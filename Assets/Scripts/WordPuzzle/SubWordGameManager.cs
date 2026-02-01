using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using VRDogVenture.Events;
using VRDogVenture.Punishment;
using VRDogVenture.Dog;

namespace VRDogVenture.WordPuzzle
{
    /// <summary>
    /// New game logic: Form sub-words from a base word.
    /// E.g., Base word "STAR" → form "ART" (3 letters), "RATS" (4 letters), etc.
    /// Auto-checks when all slots are filled. No submit button needed.
    /// </summary>
    public class SubWordGameManager : MonoBehaviour
    {
        public static SubWordGameManager Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private GameObject floatingLetterPrefab;
        [SerializeField] private GameObject socketPrefab; // Letter socket/slot

        [Header("Layout Settings")]
        [SerializeField] private Transform letterPoolCenter;
        [SerializeField] private Transform socketZoneCenter;
        [SerializeField] private float letterSpacing = 0.15f;
        [SerializeField] private float socketSpacing = 0.14f;
        [SerializeField] private float letterHeight = 1.3f;
        [SerializeField] private float socketHeight = 1.0f;
        [SerializeField] private float distanceFromPlayer = 0.8f;

        [Header("Game Data - Easy Level (3 words)")]
        [SerializeField] private List<WordChallenge> easyWords = new List<WordChallenge>
        {
            new WordChallenge("STAR", new string[] { "AT", "AS", "ART", "RAT", "TAR", "SAT", "STAR", "RATS", "ARTS", "TARS" }),
            new WordChallenge("TEAM", new string[] { "AT", "AM", "ATE", "EAT", "MAT", "MET", "TEA", "MEAT", "MATE", "TAME", "TEAM" }),
            new WordChallenge("STOP", new string[] { "TO", "SO", "TOP", "POT", "OPT", "SOT", "TOPS", "POTS", "SPOT", "STOP" })
        };

        [Header("Game Data - Medium Level (3 words)")]
        [SerializeField] private List<WordChallenge> mediumWords = new List<WordChallenge>
        {
            new WordChallenge("HEART", new string[] { "AT", "ATE", "EAT", "EAR", "ART", "HAT", "RAT", "THE", "HEAT", "RATE", "HATE", "HEAR", "EART", "HEART" }),
            new WordChallenge("STONE", new string[] { "TO", "ON", "NO", "SO", "ONE", "TON", "TEN", "SET", "NET", "NOT", "TONE", "NOSE", "NOTE", "ONES", "STONE" }),
            new WordChallenge("BREAD", new string[] { "BE", "AD", "BAD", "BAR", "BED", "RED", "ARE", "EAR", "BEAR", "BARE", "READ", "DEAR", "DARE", "BREAD" })
        };

        [Header("Game Data - Hard Level (3 words)")]
        [SerializeField] private List<WordChallenge> hardWords = new List<WordChallenge>
        {
            new WordChallenge("MASTER", new string[] { "AT", "AS", "AM", "ART", "ARM", "MAT", "RAT", "SAT", "SET", "STAR", "MARS", "MAST", "RATE", "TEAM", "STEAM", "SMART", "MASTER" }),
            new WordChallenge("GARDEN", new string[] { "AN", "AD", "AGE", "AND", "ARE", "EAR", "END", "RED", "RAN", "DEAR", "DARE", "GEAR", "READ", "RAGE", "GRADE", "RAGED", "GARDEN" }),
            new WordChallenge("PLANET", new string[] { "AN", "AT", "APE", "ANT", "ATE", "EAT", "LET", "PAN", "PEN", "PET", "TAN", "TEA", "LANE", "LATE", "LEAN", "NEAT", "PALE", "PANE", "PLAN", "PLANT", "PLANET" })
        };

        [Header("Scoring")]
        [SerializeField] private int pointsPerLetter = 10;
        [SerializeField] private int bonusPerWord = 25;

        // Runtime state
        private List<FloatingLetter> spawnedLetters = new List<FloatingLetter>();
        private List<LetterSocket> activeSockets = new List<LetterSocket>();
        private List<GameObject> socketObjects = new List<GameObject>();
        
        private WordChallenge currentChallenge;
        private int currentLevel = 1; // 1=Easy, 2=Medium, 3=Hard
        private int currentWordIndexInLevel = 0;
        private int currentTargetSlotCount = 2; // Start with 2-letter words
        private HashSet<string> wordsFoundThisChallenge = new HashSet<string>();
        private List<string> remainingWordsToFind = new List<string>();
        
        private int totalScore = 0;
        private bool isBeingPunished = false;

        // UI
        private TextMeshPro baseWordDisplay;
        private TextMeshPro targetDisplay;
        private TextMeshPro wordsFoundDisplay;

        public int TotalScore => totalScore;
        public int CurrentLevel => currentLevel;
        public string CurrentBaseWord => currentChallenge?.baseWord ?? "";

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
            SetupPositions();
            CreateUI();
            StartNewChallenge();
        }

        private void SetupPositions()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 playerPos = cam.transform.position;
            Vector3 forward = cam.transform.forward;
            forward.y = 0;
            forward.Normalize();

            // Letter pool above and in front
            if (letterPoolCenter == null)
            {
                GameObject poolObj = new GameObject("LetterPoolCenter");
                poolObj.transform.SetParent(transform);
                letterPoolCenter = poolObj.transform;
            }
            letterPoolCenter.position = playerPos + forward * distanceFromPlayer + Vector3.up * letterHeight;

            // Socket zone below letters
            if (socketZoneCenter == null)
            {
                GameObject socketObj = new GameObject("SocketZoneCenter");
                socketObj.transform.SetParent(transform);
                socketZoneCenter = socketObj.transform;
            }
            socketZoneCenter.position = playerPos + forward * distanceFromPlayer + Vector3.up * socketHeight;
        }

        private void CreateUI()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 forward = cam.transform.forward;
            forward.y = 0;
            forward.Normalize();

            // Base word display (above letters)
            GameObject baseWordObj = new GameObject("BaseWordDisplay");
            baseWordObj.transform.SetParent(transform);
            baseWordObj.transform.position = letterPoolCenter.position + Vector3.up * 0.25f;
            baseWordDisplay = baseWordObj.AddComponent<TextMeshPro>();
            baseWordDisplay.fontSize = 0.4f;
            baseWordDisplay.alignment = TextAlignmentOptions.Center;
            baseWordDisplay.color = Color.yellow;
            baseWordDisplay.fontStyle = FontStyles.Bold;

            // Target display (shows what length word to make)
            GameObject targetObj = new GameObject("TargetDisplay");
            targetObj.transform.SetParent(transform);
            targetObj.transform.position = socketZoneCenter.position + Vector3.up * 0.2f;
            targetDisplay = targetObj.AddComponent<TextMeshPro>();
            targetDisplay.fontSize = 0.2f;
            targetDisplay.alignment = TextAlignmentOptions.Center;
            targetDisplay.color = Color.cyan;

            // Words found display
            GameObject wordsFoundObj = new GameObject("WordsFoundDisplay");
            wordsFoundObj.transform.SetParent(transform);
            wordsFoundObj.transform.position = socketZoneCenter.position + Vector3.down * 0.3f;
            wordsFoundDisplay = wordsFoundObj.AddComponent<TextMeshPro>();
            wordsFoundDisplay.fontSize = 0.12f;
            wordsFoundDisplay.alignment = TextAlignmentOptions.Center;
            wordsFoundDisplay.color = Color.green;
        }

        #region Game Flow

        public void StartNewChallenge()
        {
            // Get word list for current level
            List<WordChallenge> levelWords = GetWordsForLevel(currentLevel);
            
            if (currentWordIndexInLevel >= levelWords.Count)
            {
                // Level complete! Move to next level
                currentLevel++;
                currentWordIndexInLevel = 0;
                
                if (currentLevel > 3)
                {
                    // Game complete!
                    Debug.Log("[SubWordGame] GAME COMPLETE! All levels finished!");
                    GameEvents.TriggerLevelUp(currentLevel);
                    return;
                }
                
                GameEvents.TriggerLevelUp(currentLevel);
                levelWords = GetWordsForLevel(currentLevel);
            }

            currentChallenge = levelWords[currentWordIndexInLevel];
            wordsFoundThisChallenge.Clear();
            
            // Build list of words to find, sorted by length
            remainingWordsToFind = currentChallenge.validSubWords
                .OrderBy(w => w.Length)
                .ThenBy(w => w)
                .ToList();

            // Start with shortest word length
            currentTargetSlotCount = remainingWordsToFind.Count > 0 
                ? remainingWordsToFind[0].Length 
                : 2;

            SpawnLetters();
            SpawnSockets(currentTargetSlotCount);
            UpdateUI();

            Debug.Log($"[SubWordGame] New challenge: '{currentChallenge.baseWord}' - {remainingWordsToFind.Count} words to find");
            GameEvents.TriggerNewBaseWord(currentChallenge.baseWord);
        }

        private List<WordChallenge> GetWordsForLevel(int level)
        {
            switch (level)
            {
                case 1: return easyWords;
                case 2: return mediumWords;
                case 3: return hardWords;
                default: return easyWords;
            }
        }

        private void SpawnLetters()
        {
            // Clear existing
            foreach (var letter in spawnedLetters)
            {
                if (letter != null) Destroy(letter.gameObject);
            }
            spawnedLetters.Clear();

            if (floatingLetterPrefab == null)
            {
                Debug.LogError("[SubWordGame] floatingLetterPrefab not assigned!");
                return;
            }

            string word = currentChallenge.baseWord;
            float totalWidth = (word.Length - 1) * letterSpacing;
            Vector3 startPos = letterPoolCenter.position - new Vector3(totalWidth / 2f, 0, 0);

            Camera cam = Camera.main;

            for (int i = 0; i < word.Length; i++)
            {
                Vector3 pos = startPos + new Vector3(i * letterSpacing, 0, 0);
                
                // Face the camera
                Quaternion rotation = Quaternion.identity;
                if (cam != null)
                {
                    Vector3 lookDir = cam.transform.position - pos;
                    lookDir.y = 0;
                    if (lookDir.sqrMagnitude > 0.001f)
                    {
                        rotation = Quaternion.LookRotation(-lookDir); // Face AWAY from camera so text is readable
                    }
                }

                GameObject letterObj = Instantiate(floatingLetterPrefab, pos, rotation, transform);
                FloatingLetter letter = letterObj.GetComponent<FloatingLetter>();
                
                if (letter != null)
                {
                    letter.Initialize(word[i]);
                    letter.SetFloatPosition(pos);
                    letter.SetSubWordManager(this); // Link to this manager
                    spawnedLetters.Add(letter);
                }
            }

            Debug.Log($"[SubWordGame] Spawned {word.Length} letters for '{word}'");
        }

        private void SpawnSockets(int count)
        {
            // Clear existing sockets
            foreach (var socket in socketObjects)
            {
                if (socket != null) Destroy(socket);
            }
            socketObjects.Clear();
            activeSockets.Clear();

            float totalWidth = (count - 1) * socketSpacing;
            Vector3 startPos = socketZoneCenter.position - new Vector3(totalWidth / 2f, 0, 0);

            Camera cam = Camera.main;

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = startPos + new Vector3(i * socketSpacing, 0, 0);
                
                // Create socket
                GameObject socketObj;
                if (socketPrefab != null)
                {
                    socketObj = Instantiate(socketPrefab, pos, Quaternion.identity, transform);
                }
                else
                {
                    // Create default socket (glowing cube outline)
                    socketObj = CreateDefaultSocket(pos, i);
                }

                socketObj.name = $"Socket_{i}";
                socketObjects.Add(socketObj);

                // Add LetterSocket component
                LetterSocket socket = socketObj.GetComponent<LetterSocket>();
                if (socket == null)
                {
                    socket = socketObj.AddComponent<LetterSocket>();
                }
                socket.Initialize(i, this);
                activeSockets.Add(socket);
            }

            Debug.Log($"[SubWordGame] Created {count} sockets");
        }

        private GameObject CreateDefaultSocket(Vector3 position, int index)
        {
            // Create a visible socket frame
            GameObject socketObj = new GameObject($"Socket_{index}");
            socketObj.transform.position = position;

            // Main cube (semi-transparent)
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(socketObj.transform);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale = new Vector3(0.1f, 0.1f, 0.02f);

            Renderer rend = cube.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                if (mat.shader == null) mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = new Color(0.2f, 0.7f, 1f, 0.5f); // Light blue glow
                rend.material = mat;
            }

            // Keep collider for snapping detection
            BoxCollider col = cube.GetComponent<BoxCollider>();
            if (col != null)
            {
                col.isTrigger = true;
                col.size = new Vector3(2f, 2f, 2f); // Large trigger area
            }

            return socketObj;
        }

        #endregion

        #region Letter Placement

        /// <summary>
        /// Called when a letter is placed in a socket.
        /// </summary>
        public void OnLetterPlacedInSocket(LetterSocket socket, FloatingLetter letter)
        {
            Debug.Log($"[SubWordGame] Letter '{letter.Letter}' placed in socket {socket.Index}");
            
            // Check if all sockets are filled
            if (AreAllSocketsFilled())
            {
                CheckCurrentWord();
            }
        }

        /// <summary>
        /// Called when a letter is removed from a socket.
        /// </summary>
        public void OnLetterRemovedFromSocket(LetterSocket socket)
        {
            Debug.Log($"[SubWordGame] Letter removed from socket {socket.Index}");
        }

        private bool AreAllSocketsFilled()
        {
            foreach (var socket in activeSockets)
            {
                if (!socket.HasLetter) return false;
            }
            return true;
        }

        private string GetCurrentWord()
        {
            string word = "";
            foreach (var socket in activeSockets.OrderBy(s => s.Index))
            {
                if (socket.HasLetter)
                {
                    word += socket.CurrentLetter.Letter;
                }
            }
            return word;
        }

        #endregion

        #region Word Checking

        private void CheckCurrentWord()
        {
            string word = GetCurrentWord().ToUpper();
            Debug.Log($"[SubWordGame] Checking word: '{word}'");

            // Check if it's a valid sub-word
            bool isValid = currentChallenge.validSubWords.Contains(word);
            bool alreadyFound = wordsFoundThisChallenge.Contains(word);

            if (isValid && !alreadyFound)
            {
                // CORRECT!
                OnCorrectWord(word);
            }
            else if (alreadyFound)
            {
                // Already found this word
                Debug.Log($"[SubWordGame] Word '{word}' already found!");
                ShowFeedback(false, "ALREADY FOUND!");
                // No punishment for repeated words, just clear and try again
                ClearSockets();
            }
            else
            {
                // WRONG!
                OnIncorrectWord(word);
            }
        }

        private void OnCorrectWord(string word)
        {
            Debug.Log($"[SubWordGame] CORRECT! Word: '{word}'");

            // Score
            int points = word.Length * pointsPerLetter + bonusPerWord;
            totalScore += points;

            // Track found word
            wordsFoundThisChallenge.Add(word);
            remainingWordsToFind.Remove(word);

            // Stop bees if being punished
            if (isBeingPunished)
            {
                isBeingPunished = false;
                DismissBeesWithDogBark();
            }

            // Visual feedback
            ShowFeedback(true, $"+{points}");
            foreach (var socket in activeSockets)
            {
                socket.ShowCorrect();
            }

            // Trigger events
            GameEvents.TriggerWordCorrect(word, points);
            GameEvents.TriggerScoreChanged(totalScore);

            // Move to next target
            Invoke(nameof(AdvanceToNextTarget), 1.5f);
        }

        private void OnIncorrectWord(string word)
        {
            Debug.Log($"[SubWordGame] INCORRECT! '{word}' is not a valid word from '{currentChallenge.baseWord}'");

            // Visual feedback
            ShowFeedback(false, "WRONG!");
            foreach (var socket in activeSockets)
            {
                socket.ShowIncorrect();
            }

            // Start punishment (bees keep stinging until correct word)
            if (!isBeingPunished)
            {
                isBeingPunished = true;
                StartBeePunishment();
            }

            // Trigger events
            GameEvents.TriggerWordIncorrect(word);

            // Clear sockets after delay
            Invoke(nameof(ClearSockets), 1f);
        }

        private void AdvanceToNextTarget()
        {
            ClearSockets();

            // Find next word length to target
            if (remainingWordsToFind.Count > 0)
            {
                // Get words we haven't found yet, sorted by length
                var nextWords = remainingWordsToFind
                    .Where(w => w.Length >= currentTargetSlotCount)
                    .OrderBy(w => w.Length)
                    .ToList();

                if (nextWords.Count > 0)
                {
                    // Try same length first, then increase
                    var sameLength = nextWords.Where(w => w.Length == currentTargetSlotCount).ToList();
                    if (sameLength.Count == 0)
                    {
                        // Increase slot count
                        currentTargetSlotCount = nextWords[0].Length;
                    }
                    
                    SpawnSockets(currentTargetSlotCount);
                    UpdateUI();
                    return;
                }
            }

            // No more words for this challenge - move to next base word
            currentWordIndexInLevel++;
            StartNewChallenge();
        }

        private void ClearSockets()
        {
            foreach (var socket in activeSockets)
            {
                socket.ClearLetter();
            }

            // Return letters to pool
            foreach (var letter in spawnedLetters)
            {
                if (letter != null)
                {
                    letter.ReturnToPool();
                }
            }
        }

        #endregion

        #region Punishment & Dog

        private void StartBeePunishment()
        {
            if (PunishmentSystem.Instance != null)
            {
                PunishmentSystem.Instance.StartContinuousPunishment();
            }
        }

        private void DismissBeesWithDogBark()
        {
            // Dog barks to dismiss bees
            if (DogCompanion.Instance != null)
            {
                DogCompanion.Instance.BarkToDismissBees();
            }
            
            // Also directly dismiss bees (BarkToDismissBees already calls StopContinuousPunishment)
            if (PunishmentSystem.Instance != null)
            {
                PunishmentSystem.Instance.StopContinuousPunishment();
            }

            GameEvents.TriggerDogReaction(DogReactionType.Happy);
        }

        #endregion

        #region UI

        private void UpdateUI()
        {
            if (baseWordDisplay != null)
            {
                baseWordDisplay.text = $"BASE WORD: {currentChallenge.baseWord}";
            }

            if (targetDisplay != null)
            {
                targetDisplay.text = $"Make a {currentTargetSlotCount}-letter word!";
            }

            if (wordsFoundDisplay != null)
            {
                string found = string.Join(", ", wordsFoundThisChallenge.OrderBy(w => w.Length).ThenBy(w => w));
                wordsFoundDisplay.text = $"Found: {found}";
            }

            // Face camera
            Camera cam = Camera.main;
            if (cam != null)
            {
                if (baseWordDisplay != null)
                {
                    baseWordDisplay.transform.LookAt(cam.transform);
                    baseWordDisplay.transform.Rotate(0, 180, 0);
                }
                if (targetDisplay != null)
                {
                    targetDisplay.transform.LookAt(cam.transform);
                    targetDisplay.transform.Rotate(0, 180, 0);
                }
                if (wordsFoundDisplay != null)
                {
                    wordsFoundDisplay.transform.LookAt(cam.transform);
                    wordsFoundDisplay.transform.Rotate(0, 180, 0);
                }
            }
        }

        private void ShowFeedback(bool correct, string message)
        {
            // Use FloatingPointsPopup if available
            var popup = UI.FloatingPointsPopup.Instance;
            if (popup != null)
            {
                if (correct)
                {
                    popup.ShowMessage(message, Color.green);
                }
                else
                {
                    popup.ShowMessage(message, Color.red);
                }
            }
        }

        #endregion

        private void Update()
        {
            // Keep UI facing player
            UpdateUI();
        }
    }

    /// <summary>
    /// Data class for a word challenge.
    /// </summary>
    [System.Serializable]
    public class WordChallenge
    {
        public string baseWord;
        public string[] validSubWords;

        public WordChallenge(string word, string[] subWords)
        {
            baseWord = word;
            validSubWords = subWords;
        }
    }
}
