using UnityEngine;

namespace VRProject.Dog
{
    /// <summary>
    /// Controls dog bark sounds and visual effects.
    /// </summary>
    public class DogBarkController : MonoBehaviour
    {
        [Header("Audio Clips")]
        [SerializeField] private AudioClip[] barkClips;
        [SerializeField] private AudioClip happyBarkClip;
        [SerializeField] private AudioClip alertBarkClip;
        [SerializeField] private AudioClip dismissBarkClip;
        
        [Header("Settings")]
        [SerializeField] private float barkCooldown = 0.5f;
        [SerializeField] private float barkVolume = 0.7f;
        [SerializeField] private bool createAudioSourceIfMissing = true;
        
        private AudioSource audioSource;
        private float lastBarkTime = -100f;
        
        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            
            if (audioSource == null && createAudioSourceIfMissing)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D sound
                audioSource.volume = barkVolume;
                audioSource.maxDistance = 15f;
            }
        }
        
        /// <summary>
        /// Play a random bark sound
        /// </summary>
        public void Bark()
        {
            if (Time.time - lastBarkTime < barkCooldown) return;
            
            if (barkClips != null && barkClips.Length > 0)
            {
                AudioClip clip = barkClips[Random.Range(0, barkClips.Length)];
                PlayBark(clip);
            }
            else
            {
                PlayDefaultBark();
            }
            
            lastBarkTime = Time.time;
            Debug.Log("[DogBarkController] Bark!");
        }
        
        /// <summary>
        /// Play happy bark (correct answer reaction)
        /// </summary>
        public void BarkHappy()
        {
            if (Time.time - lastBarkTime < barkCooldown) return;
            
            if (happyBarkClip != null)
            {
                PlayBark(happyBarkClip);
            }
            else if (barkClips != null && barkClips.Length > 0)
            {
                // Use regular bark with higher pitch for happy
                PlayBark(barkClips[0], 1.3f);
            }
            else
            {
                PlayDefaultBark(1.3f);
            }
            
            lastBarkTime = Time.time;
            Debug.Log("[DogBarkController] Happy bark!");
        }
        
        /// <summary>
        /// Play alert bark (wrong answer reaction)
        /// </summary>
        public void BarkAlert()
        {
            if (Time.time - lastBarkTime < barkCooldown) return;
            
            if (alertBarkClip != null)
            {
                PlayBark(alertBarkClip);
            }
            else if (barkClips != null && barkClips.Length > 0)
            {
                // Use regular bark with lower pitch for alert
                PlayBark(barkClips[0], 0.8f);
            }
            else
            {
                PlayDefaultBark(0.8f);
            }
            
            lastBarkTime = Time.time;
            Debug.Log("[DogBarkController] Alert bark!");
        }
        
        /// <summary>
        /// Play dismiss bark (to chase away bees)
        /// </summary>
        public void BarkDismiss()
        {
            // Dismiss bark bypasses cooldown - urgent!
            if (dismissBarkClip != null)
            {
                PlayBark(dismissBarkClip);
            }
            else if (barkClips != null && barkClips.Length > 0)
            {
                // Multiple quick barks
                PlayBark(barkClips[0], 1.1f);
            }
            else
            {
                PlayDefaultBark(1.1f);
            }
            
            lastBarkTime = Time.time;
            Debug.Log("[DogBarkController] Dismiss bark!");
        }
        
        private void PlayBark(AudioClip clip, float pitch = 1f)
        {
            if (audioSource == null || clip == null) return;
            
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, barkVolume);
        }
        
        private void PlayDefaultBark(float pitch = 1f)
        {
            // Create a simple procedural bark sound if no clips are assigned
            if (audioSource == null) return;
            
            // Generate simple bark waveform
            int sampleRate = 44100;
            float duration = 0.3f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                // Mix of frequencies for bark-like sound
                float wave = Mathf.Sin(2 * Mathf.PI * 150 * pitch * t) * 0.5f;
                wave += Mathf.Sin(2 * Mathf.PI * 300 * pitch * t) * 0.3f;
                wave += Mathf.Sin(2 * Mathf.PI * 450 * pitch * t) * 0.2f;
                
                // Envelope
                float envelope = Mathf.Clamp01(1f - (t / duration)) * Mathf.Clamp01(t * 20f);
                data[i] = wave * envelope;
            }
            
            AudioClip procedural = AudioClip.Create("ProceduralBark", samples, 1, sampleRate, false);
            procedural.SetData(data, 0);
            
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(procedural, barkVolume);
        }
        
        /// <summary>
        /// Set bark audio clips at runtime
        /// </summary>
        public void SetBarkClips(AudioClip[] clips)
        {
            barkClips = clips;
        }
    }
}
