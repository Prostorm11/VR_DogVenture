using UnityEngine;
using TMPro;
using System.Collections;

namespace VRProject.UI
{
    /// <summary>
    /// Creates floating point popups that appear when the player scores.
    /// Points float up and fade out.
    /// </summary>
    public class FloatingPointsPopup : MonoBehaviour
    {
        public static FloatingPointsPopup Instance { get; private set; }

        [Header("Popup Settings")]
        [SerializeField] private float floatSpeed = 0.5f;
        [SerializeField] private float fadeDuration = 1.5f;
        [SerializeField] private float startScale = 0.1f;
        [SerializeField] private float maxScale = 0.15f;

        [Header("Colors")]
        [SerializeField] private Color positiveColor = Color.green;
        [SerializeField] private Color negativeColor = Color.red;
        [SerializeField] private Color bonusColor = Color.yellow;

        [Header("Prefab")]
        [SerializeField] private GameObject popupPrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Show a floating points popup at the given position.
        /// </summary>
        public void ShowPoints(int points, Vector3 worldPosition)
        {
            StartCoroutine(CreatePopup(points, worldPosition));
        }

        /// <summary>
        /// Show points popup near the player's view.
        /// </summary>
        public void ShowPointsInView(int points)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 pos = cam.transform.position + cam.transform.forward * 1f;
                pos += cam.transform.up * 0.2f;
                ShowPoints(points, pos);
            }
        }

        /// <summary>
        /// Show a text message popup near the player's view.
        /// </summary>
        public void ShowMessage(string message, Color color)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 pos = cam.transform.position + cam.transform.forward * 1f;
                pos += cam.transform.up * 0.2f;
                StartCoroutine(CreateMessagePopup(message, color, pos));
            }
        }

        private IEnumerator CreateMessagePopup(string message, Color color, Vector3 position)
        {
            GameObject popup = new GameObject("MessagePopup");
            popup.transform.position = position;

            TextMeshPro textMesh = popup.AddComponent<TextMeshPro>();
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.fontSize = 2;
            textMesh.text = message;
            textMesh.color = color;

            Camera cam = Camera.main;
            float elapsed = 0f;
            Vector3 startPos = position;

            popup.transform.localScale = Vector3.one * startScale;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;

                popup.transform.position = startPos + Vector3.up * (floatSpeed * elapsed);
                float scale = Mathf.Lerp(startScale, maxScale, Mathf.Sin(t * Mathf.PI));
                popup.transform.localScale = Vector3.one * scale;

                Color c = color;
                c.a = 1f - t;
                textMesh.color = c;

                if (cam != null)
                {
                    popup.transform.rotation = Quaternion.LookRotation(popup.transform.position - cam.transform.position);
                }

                yield return null;
            }

            Destroy(popup);
        }

        private IEnumerator CreatePopup(int points, Vector3 position)
        {
            // Create popup object
            GameObject popup;
            TextMeshPro textMesh;

            if (popupPrefab != null)
            {
                popup = Instantiate(popupPrefab, position, Quaternion.identity);
                textMesh = popup.GetComponentInChildren<TextMeshPro>();
            }
            else
            {
                // Create at runtime
                popup = new GameObject("PointsPopup");
                popup.transform.position = position;

                textMesh = popup.AddComponent<TextMeshPro>();
                textMesh.alignment = TextAlignmentOptions.Center;
                textMesh.fontSize = 2;
            }

            // Set text and color
            string prefix = points >= 0 ? "+" : "";
            textMesh.text = prefix + points.ToString();

            if (points > 50)
            {
                textMesh.color = bonusColor;
            }
            else if (points >= 0)
            {
                textMesh.color = positiveColor;
            }
            else
            {
                textMesh.color = negativeColor;
            }

            // Make it face the camera
            Camera cam = Camera.main;

            // Animate
            float elapsed = 0f;
            Vector3 startPos = position;
            Color startColor = textMesh.color;

            popup.transform.localScale = Vector3.one * startScale;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;

                // Move up
                popup.transform.position = startPos + Vector3.up * (floatSpeed * elapsed);

                // Scale up then down
                float scale = Mathf.Lerp(startScale, maxScale, Mathf.Sin(t * Mathf.PI));
                popup.transform.localScale = Vector3.one * scale;

                // Fade out
                Color c = startColor;
                c.a = 1f - t;
                textMesh.color = c;

                // Face camera
                if (cam != null)
                {
                    popup.transform.rotation = Quaternion.LookRotation(popup.transform.position - cam.transform.position);
                }

                yield return null;
            }

            Destroy(popup);
        }
    }
}
