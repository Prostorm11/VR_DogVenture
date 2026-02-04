using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;
using VRDogVenture.Core;

namespace VRDogVenture.UI
{
    /// <summary>
    /// Creates a simple VR menu with Start Game and Quit buttons.
    /// Attach to any GameObject in your menu scene.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Menu Settings")]
        [SerializeField] private Vector3 menuOffset = new Vector3(0, 0f, 2f);
        [SerializeField] private float buttonWidth = 0.3f;
        [SerializeField] private float buttonHeight = 0.08f;
        [SerializeField] private float buttonSpacing = 0.12f;
        
        [Header("Colors")]
        [SerializeField] private Color startButtonColor = new Color(0.2f, 0.7f, 0.3f);
        [SerializeField] private Color quitButtonColor = new Color(0.7f, 0.2f, 0.2f);
        [SerializeField] private Color titleColor = Color.yellow;
        
        private Transform player;
        private GameObject menuPanel;
        
        private void Start()
        {
            // Find player
            Camera cam = Camera.main;
            if (cam != null)
            {
                player = cam.transform;
            }
            
            // Ensure GameManager exists
            if (GameManager.Instance == null)
            {
                GameObject gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
            }
            
            // Create menu after short delay to let XR initialize
            Invoke(nameof(CreateMenu), 0.5f);
        }
        
        private void CreateMenu()
        {
            if (player == null)
            {
                Camera cam = Camera.main;
                if (cam != null) player = cam.transform;
            }
            
            if (player == null)
            {
                Debug.LogError("[MainMenuUI] No camera found!");
                return;
            }
            
            // Calculate menu position
            Vector3 forward = player.forward;
            forward.y = 0;
            forward.Normalize();
            
            Vector3 menuPos = player.position + forward * menuOffset.z + Vector3.up * menuOffset.y;
            
            // Create menu panel
            menuPanel = new GameObject("MainMenuPanel");
            menuPanel.transform.position = menuPos;
            FaceCamera(menuPanel.transform);
            
            // Create background
            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "MenuBackground";
            bg.transform.SetParent(menuPanel.transform);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localRotation = Quaternion.identity;
            bg.transform.localScale = new Vector3(0.6f, 0.5f, 1f);
            Destroy(bg.GetComponent<Collider>());
            
            Material bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (bgMat.shader == null) bgMat = new Material(Shader.Find("Unlit/Color"));
            bgMat.color = new Color(0.1f, 0.12f, 0.2f, 0.95f);
            bg.GetComponent<Renderer>().material = bgMat;
            
            // Title
            CreateText(menuPanel.transform, "DOG VENTURE", new Vector3(0, 0.15f, -0.01f), 0.08f, titleColor);
            CreateText(menuPanel.transform, "VR Word Puzzle", new Vector3(0, 0.08f, -0.01f), 0.04f, Color.white);
            
            // Start button
            float startY = -0.02f;
            CreateButton(menuPanel.transform, "START GAME", new Vector3(0, startY, -0.01f), 
                startButtonColor, OnStartGame);
            
            // Quit button - positioned using buttonSpacing
            CreateButton(menuPanel.transform, "QUIT", new Vector3(0, startY - buttonSpacing, -0.01f), 
                quitButtonColor, OnQuit);
            
            Debug.Log("[MainMenuUI] Menu created");
        }
        
        private void CreateText(Transform parent, string text, Vector3 localPos, float fontSize, Color color)
        {
            GameObject textObj = new GameObject($"Text_{text}");
            textObj.transform.SetParent(parent);
            textObj.transform.localPosition = localPos;
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = Vector3.one;
            
            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
        }
        
        private void CreateButton(Transform parent, string text, Vector3 localPos, Color color, System.Action onClick)
        {
            // Button background
            GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            btn.name = $"Button_{text}";
            btn.transform.SetParent(parent);
            btn.transform.localPosition = localPos;
            btn.transform.localRotation = Quaternion.identity;
            btn.transform.localScale = new Vector3(buttonWidth, buttonHeight, 0.02f);
            
            Material btnMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (btnMat.shader == null) btnMat = new Material(Shader.Find("Standard"));
            btnMat.color = color;
            btn.GetComponent<Renderer>().material = btnMat;
            
            // Button text
            GameObject textObj = new GameObject("ButtonText");
            textObj.transform.SetParent(btn.transform);
            textObj.transform.localPosition = new Vector3(0, 0, -0.6f);
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = Vector3.one * 30f;
            
            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 1.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            
            // Make interactable
            XRSimpleInteractable interactable = btn.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener((args) => onClick?.Invoke());
        }
        
        private void OnStartGame()
        {
            Debug.Log("[MainMenuUI] Start Game clicked!");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
        }
        
        private void OnQuit()
        {
            Debug.Log("[MainMenuUI] Quit clicked!");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        
        private void FaceCamera(Transform t)
        {
            if (player != null)
            {
                Vector3 lookDir = player.position - t.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    t.rotation = Quaternion.LookRotation(-lookDir);
                }
            }
        }
    }
}
