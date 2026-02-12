using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.InputSystem;
using TMPro;
using VRProject.Core;

namespace VRProject.UI
{
    /// <summary>
    /// In-game pause menu and game over screen.
    /// Attach to any GameObject in your game scene.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("Pause Menu")]
        [SerializeField] private Key pauseKey = Key.Escape;
        
        private Transform player;
        private GameObject pauseMenu;
        private bool isPaused = false;
        
        private void Start()
        {
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
        }
        
        private void Update()
        {
            // Check for pause input (keyboard for testing, VR would use controller button)
            // Use new Input System
            if (Keyboard.current != null && Keyboard.current[pauseKey].wasPressedThisFrame)
            {
                TogglePause();
            }
        }
        
        public void TogglePause()
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
        
        public void Pause()
        {
            if (isPaused) return;
            
            isPaused = true;
            Time.timeScale = 0f;
            CreatePauseMenu();
            Debug.Log("[GameUI] Game Paused");
        }
        
        public void Resume()
        {
            if (!isPaused) return;
            
            isPaused = false;
            Time.timeScale = 1f;
            
            if (pauseMenu != null)
            {
                Destroy(pauseMenu);
            }
            Debug.Log("[GameUI] Game Resumed");
        }
        
        private void CreatePauseMenu()
        {
            if (pauseMenu != null)
            {
                Destroy(pauseMenu);
            }
            
            if (player == null)
            {
                Camera cam = Camera.main;
                if (cam != null) player = cam.transform;
            }
            
            Vector3 forward = player.forward;
            forward.y = 0;
            forward.Normalize();
            
            Vector3 menuPos = player.position + forward * 1.5f + Vector3.up * 0.2f;
            
            pauseMenu = new GameObject("PauseMenu");
            pauseMenu.transform.position = menuPos;
            FaceCamera(pauseMenu.transform);
            
            // Background
            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "PauseBG";
            bg.transform.SetParent(pauseMenu.transform);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localRotation = Quaternion.identity;
            bg.transform.localScale = new Vector3(0.5f, 0.4f, 1f);
            Destroy(bg.GetComponent<Collider>());
            
            Material bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (bgMat.shader == null) bgMat = new Material(Shader.Find("Unlit/Color"));
            bgMat.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            bg.GetComponent<Renderer>().material = bgMat;
            
            // Title
            CreateText(pauseMenu.transform, "PAUSED", new Vector3(0, 0.12f, -0.01f), 0.06f, Color.yellow);
            
            // Resume button
            CreateButton(pauseMenu.transform, "RESUME", new Vector3(0, 0.02f, -0.01f), 
                new Color(0.2f, 0.6f, 0.3f), Resume);
            
            // Restart button
            CreateButton(pauseMenu.transform, "RESTART", new Vector3(0, -0.08f, -0.01f), 
                new Color(0.6f, 0.5f, 0.2f), OnRestart);
            
            // Main Menu button
            CreateButton(pauseMenu.transform, "MAIN MENU", new Vector3(0, -0.18f, -0.01f), 
                new Color(0.5f, 0.2f, 0.2f), OnMainMenu);
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
            btn.transform.localScale = new Vector3(0.25f, 0.06f, 0.02f);
            
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
            tmp.fontSize = 1.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            
            XRSimpleInteractable interactable = btn.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener((args) => {
                Time.timeScale = 1f; // Ensure time is running for scene load
                onClick?.Invoke();
            });
        }
        
        private void OnRestart()
        {
            isPaused = false;
            if (pauseMenu != null) Destroy(pauseMenu);
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartScene();
            }
        }
        
        private void OnMainMenu()
        {
            isPaused = false;
            if (pauseMenu != null) Destroy(pauseMenu);
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GoToMainMenu();
            }
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
            // Ensure time is reset if destroyed while paused
            Time.timeScale = 1f;
        }
    }
}
