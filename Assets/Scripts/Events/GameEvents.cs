using System;

namespace VRProject.Events
{
    /// <summary>
    /// Enum for dog reaction types used throughout the game.
    /// </summary>
    public enum DogReactionType
    {
        Happy,
        Sad,
        Angry,
        Alert,
        Excited,
        Neutral
    }

    /// <summary>
    /// Central event system for game-wide communication.
    /// Subscribe to events in OnEnable, unsubscribe in OnDisable.
    /// </summary>
    public static class GameEvents
    {
        // Word Events
        public static event Action<string, int> OnWordCorrect;  // word, points
        public static event Action<string> OnWordIncorrect;     // attempted word
        public static event Action<string> OnNewBaseWord;       // new base word
        public static event Action<char> OnLetterPlaced;        // letter placed
        public static event Action<char> OnLetterRemoved;       // letter removed

        // Game State Events
        public static event Action OnGameStarted;
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;
        public static event Action OnGameEnded;
        public static event Action<int> OnLevelUp;              // new level
        public static event Action<int> OnScoreChanged;         // new score

        // Dog Events
        public static event Action<DogReactionType> OnDogReaction;

        // Trigger Methods - Word Events
        public static void TriggerWordCorrect(string word, int points)
        {
            OnWordCorrect?.Invoke(word, points);
        }

        public static void TriggerWordIncorrect(string word)
        {
            OnWordIncorrect?.Invoke(word);
        }

        public static void TriggerNewBaseWord(string word)
        {
            OnNewBaseWord?.Invoke(word);
        }

        public static void TriggerLetterPlaced(char letter)
        {
            OnLetterPlaced?.Invoke(letter);
        }

        public static void TriggerLetterRemoved(char letter)
        {
            OnLetterRemoved?.Invoke(letter);
        }

        // Trigger Methods - Game State Events
        public static void TriggerGameStarted()
        {
            OnGameStarted?.Invoke();
        }

        public static void TriggerGamePaused()
        {
            OnGamePaused?.Invoke();
        }

        public static void TriggerGameResumed()
        {
            OnGameResumed?.Invoke();
        }

        public static void TriggerGameEnded()
        {
            OnGameEnded?.Invoke();
        }

        public static void TriggerLevelUp(int level)
        {
            OnLevelUp?.Invoke(level);
        }

        public static void TriggerScoreChanged(int score)
        {
            OnScoreChanged?.Invoke(score);
        }

        // Trigger Methods - Dog Events
        public static void TriggerDogReaction(DogReactionType reactionType)
        {
            OnDogReaction?.Invoke(reactionType);
        }

        /// <summary>
        /// Clear all event subscribers. Call when restarting game or returning to menu.
        /// </summary>
        public static void ClearAllListeners()
        {
            OnWordCorrect = null;
            OnWordIncorrect = null;
            OnNewBaseWord = null;
            OnLetterPlaced = null;
            OnLetterRemoved = null;
            OnGameStarted = null;
            OnGamePaused = null;
            OnGameResumed = null;
            OnGameEnded = null;
            OnLevelUp = null;
            OnScoreChanged = null;
            OnDogReaction = null;
        }
    }
}
