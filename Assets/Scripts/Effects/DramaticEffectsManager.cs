using UnityEngine;
using System.Collections;
using TMPro;
using VRDogVenture.Events;

namespace VRDogVenture.Effects
{
    /// <summary>
    /// Creates dramatic visual and audio effects for game events.
    /// Screen flashes, particles, camera shakes, and dramatic announcements.
    /// </summary>
    public class DramaticEffectsManager : MonoBehaviour
    {
        public static DramaticEffectsManager Instance { get; private set; }

        [Header("Screen Effects")]
        [SerializeField] private Color correctFlashColor = new Color(0.2f, 1f, 0.3f, 0.3f);
        [SerializeField] private Color incorrectFlashColor = new Color(1f, 0.2f, 0.2f, 0.3f);
        [SerializeField] private Color levelUpFlashColor = new Color(1f, 0.9f, 0.2f, 0.4f);
        [SerializeField] private float flashDuration = 0.3f;

        [Header("Particle Effects")]
        [SerializeField] private GameObject confettiPrefab;
        [SerializeField] private GameObject sparklesPrefab;
        [SerializeField] private GameObject smokeburstPrefab;

        [Header("Audio - Assign these clips!")]
        [SerializeField] private AudioSource effectsAudioSource;
        
        // WORD EVENTS
        [SerializeField] private AudioClip wordCorrectSound;       // Triumphant chime/ding
        [SerializeField] private AudioClip wordIncorrectSound;     // Buzzer/fail sound
        [SerializeField] private AudioClip letterSnapSound;        // Click/snap when letter placed
        [SerializeField] private AudioClip letterGrabSound;        // Pickup sound
        
        // LEVEL/SCORE
        [SerializeField] private AudioClip levelUpFanfare;         // Epic fanfare
        [SerializeField] private AudioClip bonusPointsSound;       // Coin/bonus sound
        [SerializeField] private AudioClip comboSound;             // Increasing combo tone
        
        // DOG SOUNDS  
        [SerializeField] private AudioClip dogHappyBark;           // Happy bark
        [SerializeField] private AudioClip dogSadWhimper;          // Sad whimper
        [SerializeField] private AudioClip dogAlertBark;           // Alert bark
        [SerializeField] private AudioClip dogCheerBark;           // Celebratory bark (dismisses bees)
        
        // BEE SOUNDS
        [SerializeField] private AudioClip beeSwarmBuzz;           // Angry buzzing
        [SerializeField] private AudioClip beeStingSound;          // Sting ouch
        [SerializeField] private AudioClip beeDismissedSound;      // Bees flying away
        
        // AMBIENT/UI
        [SerializeField] private AudioClip menuOpenSound;          // UI whoosh
        [SerializeField] private AudioClip buttonClickSound;       // Click
        [SerializeField] private AudioClip ambientMusic;           // Background music loop
        [SerializeField] private AudioClip tensionMusic;           // When bees are attacking
        [SerializeField] private AudioClip victoryMusic;           // Level complete

        // Screen flash object
        private GameObject screenFlashObject;
        private Renderer screenFlashRenderer;
        private Camera playerCamera;

        // Combo tracking
        private int currentCombo = 0;
        private float lastCorrectTime = 0f;
        private float comboTimeout = 5f;

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
            playerCamera = Camera.main;
            CreateScreenFlashEffect();
            
            if (effectsAudioSource == null)
            {
                effectsAudioSource = gameObject.AddComponent<AudioSource>();
            }

            // Subscribe to events
            GameEvents.OnWordCorrect += OnWordCorrect;
            GameEvents.OnWordIncorrect += OnWordIncorrect;
            GameEvents.OnLevelUp += OnLevelUp;
            GameEvents.OnNewBaseWord += OnNewWord;
        }

        private void OnDestroy()
        {
            GameEvents.OnWordCorrect -= OnWordCorrect;
            GameEvents.OnWordIncorrect -= OnWordIncorrect;
            GameEvents.OnLevelUp -= OnLevelUp;
            GameEvents.OnNewBaseWord -= OnNewWord;
        }

        private void Update()
        {
            // Reset combo if too much time has passed
            if (currentCombo > 0 && Time.time - lastCorrectTime > comboTimeout)
            {
                currentCombo = 0;
            }
        }

        #region Event Handlers

        private void OnWordCorrect(string word, int points)
        {
            // Update combo
            currentCombo++;
            lastCorrectTime = Time.time;

            // Dramatic effects based on combo
            StartCoroutine(PlayCorrectEffects(word, points, currentCombo));
        }

        private void OnWordIncorrect(string word)
        {
            currentCombo = 0;
            StartCoroutine(PlayIncorrectEffects());
        }

        private void OnLevelUp(int level)
        {
            StartCoroutine(PlayLevelUpEffects(level));
        }

        private void OnNewWord(string word)
        {
            // Subtle announcement for new word
            PlaySound(letterSnapSound, 0.5f);
        }

        #endregion

        #region Effect Coroutines

        private IEnumerator PlayCorrectEffects(string word, int points, int combo)
        {
            // Flash screen green
            FlashScreen(correctFlashColor);

            // Play sound
            PlaySound(wordCorrectSound);

            // Combo bonus effects
            if (combo >= 3)
            {
                PlaySound(comboSound, 0.8f);
                SpawnParticles(sparklesPrefab, GetPlayerFrontPosition());
            }

            if (combo >= 5)
            {
                // EPIC combo - confetti!
                SpawnParticles(confettiPrefab, GetPlayerFrontPosition() + Vector3.up * 0.5f);
                PlaySound(bonusPointsSound);
            }

            // Shake effect for big words
            if (word.Length >= 5)
            {
                StartCoroutine(CameraShake(0.1f, 0.02f));
            }

            yield return null;
        }

        private IEnumerator PlayIncorrectEffects()
        {
            // Flash screen red
            FlashScreen(incorrectFlashColor);

            // Play buzzer
            PlaySound(wordIncorrectSound);

            // Small smoke puff
            SpawnParticles(smokeburstPrefab, GetPlayerFrontPosition());

            yield return null;
        }

        private IEnumerator PlayLevelUpEffects(int level)
        {
            // BIG flash
            FlashScreen(levelUpFlashColor, 0.5f);

            // Fanfare
            PlaySound(levelUpFanfare);

            // Confetti explosion
            SpawnParticles(confettiPrefab, GetPlayerFrontPosition() + Vector3.up);
            SpawnParticles(sparklesPrefab, GetPlayerFrontPosition());

            // Camera shake
            StartCoroutine(CameraShake(0.3f, 0.03f));

            // Create floating level up text
            CreateLevelUpAnnouncement(level);

            yield return null;
        }

        #endregion

        #region Visual Effects

        private void CreateScreenFlashEffect()
        {
            if (playerCamera == null) return;

            // Create a quad that covers the screen
            screenFlashObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screenFlashObject.name = "ScreenFlash";
            screenFlashObject.transform.SetParent(playerCamera.transform);
            screenFlashObject.transform.localPosition = new Vector3(0, 0, 0.5f);
            screenFlashObject.transform.localRotation = Quaternion.identity;
            screenFlashObject.transform.localScale = new Vector3(2f, 2f, 1f);

            // Remove collider
            Destroy(screenFlashObject.GetComponent<Collider>());

            // Setup transparent material
            screenFlashRenderer = screenFlashObject.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (mat.shader == null)
                mat = new Material(Shader.Find("Unlit/Transparent"));
            
            mat.color = Color.clear;
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0); // Alpha
            screenFlashRenderer.material = mat;

            screenFlashObject.SetActive(false);
        }

        private void FlashScreen(Color color, float duration = -1)
        {
            if (duration < 0) duration = flashDuration;
            StartCoroutine(DoScreenFlash(color, duration));
        }

        private IEnumerator DoScreenFlash(Color color, float duration)
        {
            if (screenFlashObject == null || screenFlashRenderer == null) yield break;

            screenFlashObject.SetActive(true);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Fade from color to transparent
                Color currentColor = Color.Lerp(color, Color.clear, t);
                screenFlashRenderer.material.color = currentColor;

                yield return null;
            }

            screenFlashObject.SetActive(false);
        }

        private IEnumerator CameraShake(float duration, float magnitude)
        {
            if (playerCamera == null) yield break;

            // Store original position
            Vector3 originalPos = playerCamera.transform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                playerCamera.transform.localPosition = originalPos + new Vector3(x, y, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }

            playerCamera.transform.localPosition = originalPos;
        }

        private void SpawnParticles(GameObject prefab, Vector3 position)
        {
            if (prefab == null) return;

            GameObject particles = Instantiate(prefab, position, Quaternion.identity);
            Destroy(particles, 3f); // Auto-destroy after 3 seconds
        }

        private void CreateLevelUpAnnouncement(int level)
        {
            if (playerCamera == null) return;

            Vector3 pos = GetPlayerFrontPosition() + Vector3.up * 0.3f;

            GameObject announceObj = new GameObject("LevelUpAnnounce");
            announceObj.transform.position = pos;

            TextMeshPro tmp = announceObj.AddComponent<TextMeshPro>();
            tmp.text = $"★ LEVEL {level}! ★";
            tmp.fontSize = 0.6f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = levelUpFlashColor;
            tmp.fontStyle = FontStyles.Bold;

            // Animate and destroy
            StartCoroutine(AnimateLevelUpText(announceObj, tmp));
        }

        private IEnumerator AnimateLevelUpText(GameObject obj, TextMeshPro tmp)
        {
            float duration = 2f;
            float elapsed = 0f;
            Vector3 startPos = obj.transform.position;
            Color startColor = tmp.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Float up and scale
                obj.transform.position = startPos + Vector3.up * t * 0.5f;
                float scale = 1f + Mathf.Sin(t * Mathf.PI * 4) * 0.1f; // Pulsing
                obj.transform.localScale = Vector3.one * scale;

                // Face player
                if (playerCamera != null)
                {
                    obj.transform.LookAt(playerCamera.transform);
                    obj.transform.Rotate(0, 180, 0);
                }

                // Fade out at end
                if (t > 0.7f)
                {
                    float fade = (t - 0.7f) / 0.3f;
                    tmp.color = new Color(startColor.r, startColor.g, startColor.b, 1f - fade);
                }

                yield return null;
            }

            Destroy(obj);
        }

        #endregion

        #region Audio

        private void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (clip == null || effectsAudioSource == null) return;
            effectsAudioSource.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// Play letter snap sound when a letter is placed
        /// </summary>
        public void PlayLetterSnap()
        {
            PlaySound(letterSnapSound, 0.7f);
        }

        /// <summary>
        /// Play letter grab sound when a letter is picked up
        /// </summary>
        public void PlayLetterGrab()
        {
            PlaySound(letterGrabSound, 0.6f);
        }

        #endregion

        #region Helpers

        private Vector3 GetPlayerFrontPosition()
        {
            if (playerCamera == null) playerCamera = Camera.main;
            if (playerCamera == null) return Vector3.zero;

            return playerCamera.transform.position + playerCamera.transform.forward * 1.5f;
        }

        #endregion
    }
}
