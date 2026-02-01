using System.Collections;
using UnityEngine;
using TMPro;

namespace VRDogVenture.Dog
{
    /// <summary>
    /// Controls dog bark animations and effects.
    /// Since the ithappy Dog.controller only has Vert/State parameters for movement,
    /// this script creates bark effects through:
    /// 1. Audio playback
    /// 2. Visual "WOOF!" popup
    /// 3. Head/body bobbing animation
    /// </summary>
    public class DogBarkController : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] barkClips; // Multiple bark sounds for variety
        [SerializeField] private AudioClip happyBark;
        [SerializeField] private AudioClip alertBark;
        [SerializeField] private AudioClip dismissBark; // Powerful bark to scare bees
        
        [Header("Visual Effects")]
        [SerializeField] private bool showBarkPopup = true;
        [SerializeField] private float popupDuration = 0.8f;
        [SerializeField] private float popupHeight = 0.5f;
        
        [Header("Body Animation")]
        [SerializeField] private Transform headBone; // Assign if you want head movement
        [SerializeField] private float barkBobIntensity = 0.1f;
        [SerializeField] private float barkBobSpeed = 15f;
        
        // Runtime
        private GameObject currentPopup;
        private bool isBarking = false;
        private Vector3 originalHeadRotation;
        
        private void Start()
        {
            // Get audio source
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            
            // Try to find head bone automatically
            if (headBone == null)
            {
                // Common bone names for head
                string[] headNames = { "Head", "head", "Bip001 Head", "Dog_Head", "Bone_Head" };
                foreach (var name in headNames)
                {
                    Transform found = FindDeepChild(transform, name);
                    if (found != null)
                    {
                        headBone = found;
                        break;
                    }
                }
            }
            
            if (headBone != null)
            {
                originalHeadRotation = headBone.localEulerAngles;
            }
        }
        
        /// <summary>
        /// Play a random bark.
        /// </summary>
        public void Bark()
        {
            if (isBarking) return;
            StartCoroutine(BarkSequence(GetRandomBarkClip()));
        }
        
        /// <summary>
        /// Play a happy/excited bark.
        /// </summary>
        public void BarkHappy()
        {
            if (isBarking) return;
            StartCoroutine(BarkSequence(happyBark ?? GetRandomBarkClip()));
        }
        
        /// <summary>
        /// Play an alert bark.
        /// </summary>
        public void BarkAlert()
        {
            if (isBarking) return;
            StartCoroutine(BarkSequence(alertBark ?? GetRandomBarkClip()));
        }
        
        /// <summary>
        /// Powerful bark to dismiss bees - with visual wave effect.
        /// </summary>
        public void BarkDismiss()
        {
            StartCoroutine(PowerfulBarkSequence());
        }
        
        private IEnumerator BarkSequence(AudioClip clip)
        {
            isBarking = true;
            
            // Play sound
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
            
            // Show popup
            if (showBarkPopup)
            {
                ShowBarkPopup("WOOF!", Color.white);
            }
            
            // Head bob animation
            if (headBone != null)
            {
                yield return StartCoroutine(HeadBobAnimation(0.3f));
            }
            else
            {
                // Body bob if no head bone
                yield return StartCoroutine(BodyBobAnimation(0.3f));
            }
            
            isBarking = false;
        }
        
        private IEnumerator PowerfulBarkSequence()
        {
            isBarking = true;
            
            // Play powerful bark sound (or multiple barks)
            AudioClip clip = dismissBark ?? happyBark ?? GetRandomBarkClip();
            if (clip != null && audioSource != null)
            {
                audioSource.pitch = 0.9f; // Slightly deeper
                audioSource.PlayOneShot(clip);
            }
            
            // Show big popup
            if (showBarkPopup)
            {
                ShowBarkPopup("WOOF WOOF!", Color.yellow, 1.5f);
            }
            
            // Create bark wave visual
            CreateBarkWave();
            
            // More intense animation
            if (headBone != null)
            {
                yield return StartCoroutine(HeadBobAnimation(0.5f, 2f));
            }
            else
            {
                yield return StartCoroutine(BodyBobAnimation(0.5f, 2f));
            }
            
            audioSource.pitch = 1f;
            isBarking = false;
        }
        
        private IEnumerator HeadBobAnimation(float duration, float intensityMultiplier = 1f)
        {
            float elapsed = 0f;
            float intensity = barkBobIntensity * intensityMultiplier;
            
            while (elapsed < duration)
            {
                float bob = Mathf.Sin(elapsed * barkBobSpeed) * intensity;
                headBone.localEulerAngles = originalHeadRotation + new Vector3(bob * 30f, 0, 0);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            headBone.localEulerAngles = originalHeadRotation;
        }
        
        private IEnumerator BodyBobAnimation(float duration, float intensityMultiplier = 1f)
        {
            float elapsed = 0f;
            Vector3 originalPos = transform.localPosition;
            float intensity = barkBobIntensity * intensityMultiplier;
            
            while (elapsed < duration)
            {
                float bob = Mathf.Sin(elapsed * barkBobSpeed) * intensity;
                transform.localPosition = originalPos + new Vector3(0, bob * 0.2f, bob * 0.1f);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            transform.localPosition = originalPos;
        }
        
        private void ShowBarkPopup(string text, Color color, float sizeMultiplier = 1f)
        {
            // Destroy existing popup
            if (currentPopup != null)
            {
                Destroy(currentPopup);
            }
            
            // Create popup above dog
            currentPopup = new GameObject("BarkPopup");
            currentPopup.transform.position = transform.position + Vector3.up * popupHeight;
            
            // Add TextMeshPro
            TextMeshPro tmp = currentPopup.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 2f * sizeMultiplier;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            
            // Face camera
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 lookDir = cam.transform.position - currentPopup.transform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    currentPopup.transform.rotation = Quaternion.LookRotation(-lookDir);
                }
            }
            
            // Animate and destroy
            StartCoroutine(AnimatePopup(currentPopup, popupDuration));
        }
        
        private IEnumerator AnimatePopup(GameObject popup, float duration)
        {
            if (popup == null) yield break;
            
            Vector3 startPos = popup.transform.position;
            Vector3 endPos = startPos + Vector3.up * 0.3f;
            float elapsed = 0f;
            
            TextMeshPro tmp = popup.GetComponent<TextMeshPro>();
            Color startColor = tmp != null ? tmp.color : Color.white;
            
            while (elapsed < duration && popup != null)
            {
                float t = elapsed / duration;
                popup.transform.position = Vector3.Lerp(startPos, endPos, t);
                
                // Fade out in last 30%
                if (t > 0.7f && tmp != null)
                {
                    float fadeT = (t - 0.7f) / 0.3f;
                    tmp.color = new Color(startColor.r, startColor.g, startColor.b, 1f - fadeT);
                }
                
                // Scale pulse
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
                popup.transform.localScale = Vector3.one * scale;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (popup != null)
            {
                Destroy(popup);
            }
        }
        
        private void CreateBarkWave()
        {
            // Create expanding ring effect
            GameObject wave = new GameObject("BarkWave");
            wave.transform.position = transform.position + Vector3.up * 0.3f;
            
            // Create ring mesh (simplified - just a scaled sphere)
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ring.transform.SetParent(wave.transform);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localScale = Vector3.one * 0.1f;
            
            // Remove collider
            Destroy(ring.GetComponent<Collider>());
            
            // Semi-transparent material
            Renderer rend = ring.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                if (mat.shader == null) mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = new Color(1f, 0.8f, 0.2f, 0.5f);
                rend.material = mat;
            }
            
            // Animate wave
            StartCoroutine(AnimateBarkWave(ring.transform));
        }
        
        private IEnumerator AnimateBarkWave(Transform wave)
        {
            float duration = 0.5f;
            float elapsed = 0f;
            
            Renderer rend = wave.GetComponent<Renderer>();
            Color startColor = rend != null ? rend.material.color : Color.yellow;
            
            while (elapsed < duration && wave != null)
            {
                float t = elapsed / duration;
                
                // Expand
                float size = Mathf.Lerp(0.1f, 3f, t);
                wave.localScale = new Vector3(size, 0.05f, size);
                
                // Fade out
                if (rend != null)
                {
                    rend.material.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1f - t));
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (wave != null)
            {
                Destroy(wave.parent.gameObject);
            }
        }
        
        private AudioClip GetRandomBarkClip()
        {
            if (barkClips == null || barkClips.Length == 0)
            {
                return happyBark ?? alertBark;
            }
            return barkClips[Random.Range(0, barkClips.Length)];
        }
        
        private Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Contains(name))
                    return child;
                    
                Transform found = FindDeepChild(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
