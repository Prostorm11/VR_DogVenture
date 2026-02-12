using UnityEngine;
using UnityEngine.SceneManagement;
using VRProject.Events;
using VRProject.WordPuzzle;
using VRProject.Punishment;
using VRProject.UI;
using VRProject.Dog;

namespace VRProject.Core
{
    /// <summary>
    /// Main game manager that controls game flow, scoring, and level progression.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private int pointsPerLetter = 10;
        [SerializeField] private int bonusPointsPerWord = 50;
        [SerializeField] private int wordsPerLevel = 3; // Complete 3 words to level up
        [SerializeField] private bool autoStartOnSceneLoad = true;

        [Header("References")]
        [SerializeField] private WordValidator wordValidator;
        [SerializeField] private FloatingWordBuilder wordBuilder;

        [Header("Auto-Create Systems")]
        [SerializeField] private bool autoCreatePunishmentSystem = true;
        [SerializeField] private bool autoCreateGameMenu = true;

        // Game State
        private int currentScore = 0;
        private int currentLevel = 1;
        private int wordsCompletedInLevel = 0;
        private string currentBaseWord;
        private bool isGameActive = false;

        public int CurrentScore => currentScore;
        public int CurrentLevel => currentLevel;
        public string CurrentBaseWord => currentBaseWord;
        public bool IsGameActive => isGameActive;

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Subscribe to scene loaded event
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Find references in new scene
            if (wordValidator == null)
                wordValidator = FindAnyObjectByType<WordValidator>();
            if (wordBuilder == null)
                wordBuilder = FindAnyObjectByType<FloatingWordBuilder>();

            // Auto-create required systems in game scene
            if (scene.name == "Sample Scene" || scene.name == "infernoscene")
            {
                EnsureRequiredSystems();
            }

            // Auto-start if this is the game scene and autoStart is enabled
            if (autoStartOnSceneLoad && (scene.name == "Sample Scene" || scene.name == "infernoscene"))
            {
                StartGame();
            }
        }

        /// <summary>
        /// Ensure all required game systems exist in the scene.
        /// </summary>
        private void EnsureRequiredSystems()
        {
            Debug.Log("[GameManager] EnsureRequiredSystems called - setting up game systems...");
            
            // PunishmentSystem
            if (autoCreatePunishmentSystem)
            {
                if (PunishmentSystem.Instance == null)
                {
                    GameObject psObj = new GameObject("PunishmentSystem");
                    psObj.AddComponent<PunishmentSystem>();
                    Debug.Log("[GameManager] Created PunishmentSystem");
                }
                else
                {
                    Debug.Log("[GameManager] PunishmentSystem already exists");
                }
            }

            // GameMenuUI
            if (autoCreateGameMenu && FindAnyObjectByType<GameMenuUI>() == null)
            {
                GameObject menuObj = new GameObject("GameMenuUI");
                menuObj.AddComponent<GameMenuUI>();
                Debug.Log("[GameManager] Created GameMenuUI");
            }

            // SoundManager
            if (SoundManager.Instance == null)
            {
                GameObject soundObj = new GameObject("SoundManager");
                soundObj.AddComponent<SoundManager>();
                soundObj.AddComponent<AudioSource>();
                Debug.Log("[GameManager] Created SoundManager");
            }
            
            // DogCompanion
            if (DogCompanion.Instance == null)
            {
                Camera mainCam = Camera.main;
                Vector3 spawnPos = mainCam != null 
                    ? mainCam.transform.position + mainCam.transform.forward * 1.5f + mainCam.transform.right * 0.5f
                    : new Vector3(0, 0, 2);
                spawnPos.y = 0f;
                
                GameObject dogObj = new GameObject("DogCompanion");
                dogObj.transform.position = spawnPos;
                dogObj.AddComponent<DogCompanion>();
                
                // Add a simple visual placeholder (capsule shaped like a dog body)
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "DogBody";
                body.transform.SetParent(dogObj.transform);
                body.transform.localPosition = new Vector3(0, 0.25f, 0);
                body.transform.localScale = new Vector3(0.25f, 0.15f, 0.4f);
                body.transform.localRotation = Quaternion.Euler(90, 0, 0);
                Object.Destroy(body.GetComponent<Collider>());
                
                // Head
                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "DogHead";
                head.transform.SetParent(dogObj.transform);
                head.transform.localPosition = new Vector3(0, 0.3f, 0.25f);
                head.transform.localScale = new Vector3(0.2f, 0.18f, 0.22f);
                Object.Destroy(head.GetComponent<Collider>());
                
                // Color brown
                Material dogMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                dogMat.color = new Color(0.55f, 0.35f, 0.2f);
                body.GetComponent<Renderer>().material = dogMat;
                head.GetComponent<Renderer>().material = dogMat;
                
                Debug.Log($"[GameManager] Created DogCompanion at {spawnPos}");
            }
            else
            {
                Debug.Log("[GameManager] DogCompanion already exists");
            }
        }

        private void Start()
        {
            // Don't auto-start here - wait for scene load or manual call
        }

        public void StartGame()
        {
            currentScore = 0;
            currentLevel = 1;
            wordsCompletedInLevel = 0;
            isGameActive = true;

            GameEvents.TriggerGameStarted();
            GameEvents.TriggerScoreChanged(currentScore);
            
            LoadNewBaseWord();
        }

        public void PauseGame()
        {
            isGameActive = false;
            GameEvents.TriggerGamePaused();
        }

        public void ResumeGame()
        {
            isGameActive = true;
            GameEvents.TriggerGameResumed();
        }

        public void EndGame()
        {
            isGameActive = false;
            GameEvents.TriggerGameEnded();
            Debug.Log($"Game Over! Final Score: {currentScore}");
        }

        /// <summary>
        /// Called when player submits a word attempt.
        /// </summary>
        public void SubmitWord(string attemptedWord)
        {
            if (!isGameActive) return;

            attemptedWord = attemptedWord.ToUpper();

            // Check if word is valid
            if (wordValidator != null && wordValidator.IsValidWord(attemptedWord, currentBaseWord))
            {
                OnCorrectWord(attemptedWord);
            }
            else
            {
                OnIncorrectWord(attemptedWord);
            }
        }

        private void OnCorrectWord(string word)
        {
            // Calculate points
            int letterPoints = word.Length * pointsPerLetter;
            int totalPoints = letterPoints + bonusPointsPerWord;

            // Update score
            currentScore += totalPoints;
            wordsCompletedInLevel++;

            // Show feedback
            if (wordBuilder != null)
            {
                wordBuilder.ShowAnswerFeedback(true);
            }

            // Trigger events
            GameEvents.TriggerWordCorrect(word, totalPoints);
            GameEvents.TriggerScoreChanged(currentScore);

            // Check for level up
            if (wordsCompletedInLevel >= wordsPerLevel)
            {
                LevelUp();
            }

            // Load new word after delay (for animations)
            Invoke(nameof(LoadNewBaseWord), 2f);
        }

        private void OnIncorrectWord(string word)
        {
            // Show feedback
            if (wordBuilder != null)
            {
                wordBuilder.ShowAnswerFeedback(false);
            }

            GameEvents.TriggerWordIncorrect(word);
        }

        private void LevelUp()
        {
            currentLevel++;
            wordsCompletedInLevel = 0;
            
            // Increase difficulty
            pointsPerLetter += 5;
            
            GameEvents.TriggerLevelUp(currentLevel);
        }

        private void LoadNewBaseWord()
        {
            if (wordBuilder != null)
            {
                currentBaseWord = wordBuilder.GetNextWord(currentLevel);
                wordBuilder.SpawnWord(currentBaseWord);
            }
            else
            {
                Debug.LogWarning("WordSpawner not assigned to GameManager!");
            }
        }

        /// <summary>
        /// Reset the current word (clear placed letters).
        /// </summary>
        public void ResetCurrentWord()
        {
            if (wordBuilder != null)
            {
                wordBuilder.ClearAnswer();
            }
        }

        /// <summary>
        /// Deduct points from score (used by PunishmentSystem).
        /// </summary>
        public void DeductPoints(int points)
        {
            currentScore = Mathf.Max(0, currentScore - points);
            GameEvents.TriggerScoreChanged(currentScore);
            Debug.Log($"[GameManager] Deducted {points} points. New score: {currentScore}");
        }

        /// <summary>
        /// Add points to score.
        /// </summary>
        public void AddPoints(int points)
        {
            currentScore += points;
            GameEvents.TriggerScoreChanged(currentScore);
            Debug.Log($"[GameManager] Added {points} points. New score: {currentScore}");
        }

        /// <summary>
        /// Restart the current scene.
        /// </summary>
        public void RestartScene()
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Debug.Log($"[GameManager] Restarting scene: {currentScene}");
            UnityEngine.SceneManagement.SceneManager.LoadScene(currentScene);
        }

        /// <summary>
        /// Go to the main menu scene.
        /// </summary>
        public void GoToMainMenu()
        {
            Debug.Log("[GameManager] Going to main menu");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Sample Scene");
        }
    }
}
