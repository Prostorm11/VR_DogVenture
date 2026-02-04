using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.InputSystem;
using TMPro;
using VRDogVenture.Core;

namespace VRDogVenture.UI
{
    /// <summary>
    /// In-game menu UI with pause, restart, hints, and main menu options.
    /// </summary>
    public class GameMenuUI : MonoBehaviour
    {
        [Header("Menu Toggle")]
        [SerializeField] private Key menuKey = Key.Escape;
        [SerializeField] private bool startHidden = true;
        
        [Header("Button Colors")]
        [SerializeField] private Color resumeColor = new Color(0.2f, 0.6f, 0.3f);
        [SerializeField] private Color restartColor = new Color(0.6f, 0.5f, 0.2f);
        [SerializeField] private Color hintColor = new Color(0.3f, 0.5f, 0.7f);
        [SerializeField] private Color menuColor = new Color(0.5f, 0.2f, 0.2f);
        [SerializeField] private Color quitColor = new Color(0.4f, 0.2f, 0.2f);
        
        private Transform player;
        private GameObject menuPanel;
        private bool isMenuVisible = false;
        private bool isPaused = false;
        
        /// <summary>
        /// Returns true if the game is currently paused via this menu
        /// </summary>
        public bool IsPaused => isPaused;
        
        private void Start()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                player = cam.transform;
            }
            
            if (!startHidden)
            {
                ShowMenu();
            }
            
            Debug.Log("[GameMenuUI] Ready");
        }
        
        private void Update()
        {
            // Use new Input System
            if (Keyboard.current != null && Keyboard.current[menuKey].wasPressedThisFrame)
            {
                ToggleMenu();
            }
        }
        
        public void ToggleMenu()
        {
            if (isMenuVisible)
            {
                HideMenu();
            }
            else
            {
                ShowMenu();
            }
        }
        
        public void ShowMenu()
        {
            if (menuPanel != null)
            {
                Destroy(menuPanel);
            }
            
            CreateMenu();
            isMenuVisible = true;
            PauseGame();
        }
        
        public void HideMenu()
        {
            if (menuPanel != null)
            {
                Destroy(menuPanel);
            }
            isMenuVisible = false;
            ResumeGame();
        }
        
        private void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;
        }
        
        private void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;
        }
        
        private void CreateMenu()
        {
            if (player == null)
            {
                Camera cam = Camera.main;
                if (cam != null) player = cam.transform;
            }
            
            Vector3 forward = player.forward;
            forward.y = 0;
            forward.Normalize();
            
            Vector3 menuPos = player.position + forward * 1.5f + Vector3.up * 0.1f;
            
            menuPanel = new GameObject("GameMenuPanel");
            menuPanel.transform.position = menuPos;
            FaceCamera(menuPanel.transform);
            
            // Background
            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "MenuBG";
            bg.transform.SetParent(menuPanel.transform);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localRotation = Quaternion.identity;
            bg.transform.localScale = new Vector3(0.5f, 0.55f, 1f);
            Destroy(bg.GetComponent<Collider>());
            
            Material bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (bgMat.shader == null) bgMat = new Material(Shader.Find("Unlit/Color"));
            bgMat.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            bg.GetComponent<Renderer>().material = bgMat;
            
            // Title
            CreateText(menuPanel.transform, "GAME MENU", new Vector3(0, 0.2f, -0.01f), 0.05f, Color.yellow);
            
            // Buttons
            float yPos = 0.1f;
            float spacing = 0.08f;
            
            CreateButton(menuPanel.transform, "RESUME", new Vector3(0, yPos, -0.01f), resumeColor, OnResume);
            yPos -= spacing;
            
            CreateButton(menuPanel.transform, "GET HINT", new Vector3(0, yPos, -0.01f), hintColor, OnHint);
            yPos -= spacing;
            
            CreateButton(menuPanel.transform, "RESTART", new Vector3(0, yPos, -0.01f), restartColor, OnRestart);
            yPos -= spacing;
            
            CreateButton(menuPanel.transform, "MAIN MENU", new Vector3(0, yPos, -0.01f), menuColor, OnMainMenu);
            yPos -= spacing;
            
            CreateButton(menuPanel.transform, "QUIT", new Vector3(0, yPos, -0.01f), quitColor, OnQuit);
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
            GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            btn.name = $"Button_{text}";
            btn.transform.SetParent(parent);
            btn.transform.localPosition = localPos;
            btn.transform.localRotation = Quaternion.identity;
            btn.transform.localScale = new Vector3(0.25f, 0.05f, 0.02f);
            
            Material btnMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (btnMat.shader == null) btnMat = new Material(Shader.Find("Standard"));
            btnMat.color = color;
            btn.GetComponent<Renderer>().material = btnMat;
            
            GameObject textObj = new GameObject("ButtonText");
            textObj.transform.SetParent(btn.transform);
            textObj.transform.localPosition = new Vector3(0, 0, -0.6f);
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = Vector3.one * 25f;
            
            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 1.2f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            
            XRSimpleInteractable interactable = btn.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener((args) => {
                Time.timeScale = 1f;
                onClick?.Invoke();
            });
        }
        
        private void OnResume()
        {
            HideMenu();
        }
        
        private void OnHint()
        {
            HideMenu();
            
            // Request hint from HintSystem
            if (VRDogVenture.WordPuzzle.HintSystem.Instance != null)
            {
                VRDogVenture.WordPuzzle.HintSystem.Instance.RequestHint();
            }
            else
            {
                Debug.Log("[GameMenuUI] HintSystem not found");
            }
        }
        
        private void OnRestart()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartScene();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }
        
        private void OnMainMenu()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GoToMainMenu();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
            }
        }
        
        private void OnQuit()
        {
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
        
        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
