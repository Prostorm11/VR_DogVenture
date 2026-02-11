using UnityEngine;

namespace VRProject.Core
{
    /// <summary>
    /// Centralized sound manager for playing game audio.
    /// Handles all sound effects and music throughout the game.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;

        [Header("Word Sounds")]
        [SerializeField] private AudioClip wordCorrectSound;
        [SerializeField] private AudioClip wordIncorrectSound;
        [SerializeField] private AudioClip letterSnapSound;
        [SerializeField] private AudioClip letterGrabSound;

        [Header("Game Sounds")]
        [SerializeField] private AudioClip levelUpSound;
        [SerializeField] private AudioClip gameStartSound;
        [SerializeField] private AudioClip gameOverSound;
        [SerializeField] private AudioClip buttonClickSound;

        [Header("Bee Sounds")]
        [SerializeField] private AudioClip beeSwarmSound;
        [SerializeField] private AudioClip beeStingSound;
        [SerializeField] private AudioClip beeDismissedSound;

        [Header("Dog Sounds")]
        [SerializeField] private AudioClip dogBarkSound;
        [SerializeField] private AudioClip dogWhimperSound;
        [SerializeField] private AudioClip dogHappySound;

        [Header("Misc Sounds")]
        [SerializeField] private AudioClip ouchSound;

        [Header("Settings")]
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private float musicVolume = 0.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Setup audio sources
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
            }

            sfxSource.volume = sfxVolume;
            musicSource.volume = musicVolume;

            Debug.Log("[SoundManager] Initialized");
        }

        #region Play Methods

        public void PlaySound(AudioClip clip, float volumeScale = 1f)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, volumeScale);
            }
        }

        public void PlayWordCorrect()
        {
            PlaySound(wordCorrectSound);
        }

        public void PlayWordIncorrect()
        {
            PlaySound(wordIncorrectSound);
        }

        public void PlayLetterSnap()
        {
            PlaySound(letterSnapSound);
        }

        public void PlayLetterGrab()
        {
            PlaySound(letterGrabSound);
        }

        public void PlayLevelUp()
        {
            PlaySound(levelUpSound);
        }

        public void PlayGameStart()
        {
            PlaySound(gameStartSound);
        }

        public void PlayGameOver()
        {
            PlaySound(gameOverSound);
        }

        public void PlayButtonClick()
        {
            PlaySound(buttonClickSound);
        }

        public void PlayBeeSwarm()
        {
            PlaySound(beeSwarmSound);
        }

        public void PlayBeeSting()
        {
            PlaySound(beeStingSound);
        }

        public void PlayBeeDismissed()
        {
            PlaySound(beeDismissedSound);
        }

        public void PlayDogBark()
        {
            PlaySound(dogBarkSound);
        }

        public void PlayDogWhimper()
        {
            PlaySound(dogWhimperSound);
        }

        public void PlayDogHappy()
        {
            PlaySound(dogHappySound);
        }

        public void PlayOuch()
        {
            PlaySound(ouchSound);
        }

        #endregion

        #region Music Methods

        public void PlayMusic(AudioClip music)
        {
            if (musicSource != null && music != null)
            {
                musicSource.clip = music;
                musicSource.Play();
            }
        }

        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            if (sfxSource != null)
            {
                sfxSource.volume = sfxVolume;
            }
        }

        #endregion
    }
}
