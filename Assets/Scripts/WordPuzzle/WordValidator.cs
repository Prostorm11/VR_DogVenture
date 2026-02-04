using System.Collections.Generic;
using UnityEngine;

namespace VRDogVenture.WordPuzzle
{
    /// <summary>
    /// Validates words against a dictionary of valid sub-words for each base word.
    /// </summary>
    public class WordValidator : MonoBehaviour
    {
        public static WordValidator Instance { get; private set; }
        
        [Header("Settings")]
        [SerializeField] private bool caseSensitive = false;
        
        // Dictionary of base words and their valid sub-words
        private Dictionary<string, HashSet<string>> validSubWords = new Dictionary<string, HashSet<string>>();
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            InitializeWordLists();
            Debug.Log("[WordValidator] Ready");
        }
        
        private void InitializeWordLists()
        {
            // STAR
            AddWordList("STAR", new[] { "AT", "AS", "ART", "RAT", "TAR", "SAT", "STAR", "RATS", "ARTS", "TARS", "A", "S", "T", "R" });
            
            // TEAM
            AddWordList("TEAM", new[] { "AT", "AM", "ATE", "EAT", "MAT", "MET", "TEA", "MEAT", "MATE", "TAME", "TEAM", "A", "E", "T", "M" });
            
            // STOP
            AddWordList("STOP", new[] { "TO", "SO", "TOP", "POT", "OPT", "SOT", "TOPS", "POTS", "SPOT", "STOP", "POST", "S", "T", "O", "P" });
            
            // HEART
            AddWordList("HEART", new[] { "AT", "ATE", "EAT", "EAR", "ART", "HAT", "RAT", "THE", "HEAT", "RATE", "HATE", "HEAR", "HARE", "TEAR", "EARTH", "HEART", "A", "E", "H", "R", "T" });
            
            // STONE
            AddWordList("STONE", new[] { "TO", "ON", "NO", "SO", "ONE", "TON", "TEN", "SET", "NET", "NOT", "TONE", "NOSE", "NOTE", "ONES", "TENS", "TONES", "STONE", "NOTES", "ONSET", "S", "T", "O", "N", "E" });
            
            // DREAM
            AddWordList("DREAM", new[] { "AM", "ARE", "EAR", "ERA", "MAD", "DAM", "ARM", "RED", "READ", "MADE", "DEAR", "DARE", "MARE", "DREAM", "ARMED", "D", "R", "E", "A", "M" });
            
            // PLANT
            AddWordList("PLANT", new[] { "AT", "AN", "TAN", "PAN", "PAT", "TAP", "NAP", "LAP", "ANT", "PLAN", "PANT", "PLANT", "P", "L", "A", "N", "T" });
            
            // SMILE
            AddWordList("SMILE", new[] { "IS", "ME", "LIE", "LIES", "MILE", "SLIM", "LIME", "SLIME", "MILES", "LIMES", "SMILE", "S", "M", "I", "L", "E" });
        }
        
        private void AddWordList(string baseWord, string[] subWords)
        {
            string key = caseSensitive ? baseWord : baseWord.ToUpper();
            HashSet<string> wordSet = new HashSet<string>();
            
            foreach (string word in subWords)
            {
                wordSet.Add(caseSensitive ? word : word.ToUpper());
            }
            
            validSubWords[key] = wordSet;
        }
        
        /// <summary>
        /// Check if a word is valid for the given base word
        /// </summary>
        public bool IsValidWord(string baseWord, string wordToCheck)
        {
            if (string.IsNullOrEmpty(baseWord) || string.IsNullOrEmpty(wordToCheck))
                return false;
            
            string key = caseSensitive ? baseWord : baseWord.ToUpper();
            string check = caseSensitive ? wordToCheck : wordToCheck.ToUpper();
            
            if (!validSubWords.TryGetValue(key, out HashSet<string> validWords))
            {
                Debug.LogWarning($"[WordValidator] No word list found for base word: {baseWord}");
                return false;
            }
            
            return validWords.Contains(check);
        }
        
        /// <summary>
        /// Get all valid words for a base word
        /// </summary>
        public string[] GetValidWords(string baseWord)
        {
            string key = caseSensitive ? baseWord : baseWord.ToUpper();
            
            if (!validSubWords.TryGetValue(key, out HashSet<string> validWords))
            {
                return new string[0];
            }
            
            string[] result = new string[validWords.Count];
            validWords.CopyTo(result);
            return result;
        }
        
        /// <summary>
        /// Get valid words of a specific length for a base word
        /// </summary>
        public List<string> GetValidWordsOfLength(string baseWord, int length)
        {
            List<string> result = new List<string>();
            string key = caseSensitive ? baseWord : baseWord.ToUpper();
            
            if (!validSubWords.TryGetValue(key, out HashSet<string> validWords))
            {
                return result;
            }
            
            foreach (string word in validWords)
            {
                if (word.Length == length)
                {
                    result.Add(word);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Check if the letters can form the word (uses only available letters)
        /// </summary>
        public bool CanFormWord(string availableLetters, string wordToCheck)
        {
            if (string.IsNullOrEmpty(availableLetters) || string.IsNullOrEmpty(wordToCheck))
                return false;
            
            string letters = caseSensitive ? availableLetters : availableLetters.ToUpper();
            string word = caseSensitive ? wordToCheck : wordToCheck.ToUpper();
            
            // Count available letters
            Dictionary<char, int> letterCounts = new Dictionary<char, int>();
            foreach (char c in letters)
            {
                if (letterCounts.ContainsKey(c))
                    letterCounts[c]++;
                else
                    letterCounts[c] = 1;
            }
            
            // Check if we have enough of each letter
            foreach (char c in word)
            {
                if (!letterCounts.ContainsKey(c) || letterCounts[c] <= 0)
                    return false;
                letterCounts[c]--;
            }
            
            return true;
        }
        
        /// <summary>
        /// Add a custom word list for a base word
        /// </summary>
        public void AddCustomWordList(string baseWord, string[] validWords)
        {
            AddWordList(baseWord, validWords);
        }
    }
}
