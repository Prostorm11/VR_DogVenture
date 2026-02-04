using System.Collections.Generic;
using UnityEngine;

namespace VRDogVenture.WordPuzzle
{
    /// <summary>
    /// Generates textures for letter cubes with the letter displayed on all faces.
    /// Singleton - access via LetterTextureGenerator.Instance
    /// </summary>
    public class LetterTextureGenerator : MonoBehaviour
    {
        public static LetterTextureGenerator Instance { get; private set; }
        
        [Header("Texture Settings")]
        [SerializeField] private int textureSize = 256;
        [SerializeField] private Color backgroundColor = new Color(1f, 0.95f, 0.85f); // Cream
        [SerializeField] private Color textColor = new Color(0.2f, 0.15f, 0.1f); // Dark brown
        [SerializeField] private Font letterFont;
        
        [Header("Letter Appearance")]
        [SerializeField] private int fontSize = 180;
        [SerializeField] private FontStyle fontStyle = FontStyle.Bold;
        
        /// <summary>
        /// Get the configured font size for letter textures
        /// </summary>
        public int FontSize => fontSize;
        
        /// <summary>
        /// Get the configured font style for letter textures
        /// </summary>
        public FontStyle LetterFontStyle => fontStyle;
        
        // Cache generated materials
        private Dictionary<char, Material> letterMaterials = new Dictionary<char, Material>();
        private Dictionary<char, Texture2D> letterTextures = new Dictionary<char, Texture2D>();
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Use default font if none assigned
            if (letterFont == null)
            {
                letterFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            
            Debug.Log("[LetterTextureGenerator] Ready");
        }
        
        /// <summary>
        /// Get a material with the letter texture for a letter cube
        /// </summary>
        public Material GetMaterialForLetter(char letter)
        {
            char upperLetter = char.ToUpper(letter);
            
            // Return cached material if exists
            if (letterMaterials.TryGetValue(upperLetter, out Material cachedMat))
            {
                return cachedMat;
            }
            
            // Generate new material
            Material mat = CreateLetterMaterial(upperLetter);
            letterMaterials[upperLetter] = mat;
            return mat;
        }
        
        /// <summary>
        /// Get just the texture for a letter
        /// </summary>
        public Texture2D GetTextureForLetter(char letter)
        {
            char upperLetter = char.ToUpper(letter);
            
            if (letterTextures.TryGetValue(upperLetter, out Texture2D cachedTex))
            {
                return cachedTex;
            }
            
            Texture2D tex = GenerateLetterTexture(upperLetter);
            letterTextures[upperLetter] = tex;
            return tex;
        }
        
        private Material CreateLetterMaterial(char letter)
        {
            // Get or create texture
            Texture2D tex = GetTextureForLetter(letter);
            
            // Create material
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            
            Material mat = new Material(shader);
            mat.mainTexture = tex;
            mat.SetFloat("_Smoothness", 0.5f);
            mat.SetFloat("_Metallic", 0f);
            
            return mat;
        }
        
        private Texture2D GenerateLetterTexture(char letter)
        {
            // Create texture
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, true);
            
            // Fill with background color
            Color[] pixels = new Color[textureSize * textureSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = backgroundColor;
            }
            texture.SetPixels(pixels);
            
            // Create render texture to draw text
            RenderTexture rt = RenderTexture.GetTemporary(textureSize, textureSize, 0);
            RenderTexture.active = rt;
            
            // Clear with background
            GL.Clear(true, true, backgroundColor);
            
            // Draw letter using GUI
            // Note: This is a simplified approach - in production you'd use a proper text rendering system
            
            // For now, we'll create a simple procedural letter texture
            DrawLetterOnTexture(texture, letter);
            
            texture.Apply();
            
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            
            return texture;
        }
        
        private void DrawLetterOnTexture(Texture2D texture, char letter)
        {
            // Simple pixel-based letter drawing
            // This creates a basic but readable letter on the texture
            
            int size = textureSize;
            int margin = size / 8;
            int letterWidth = size - (margin * 2);
            int letterHeight = size - (margin * 2);
            
            // Get letter pattern (simplified - just draws basic shapes)
            bool[,] pattern = GetLetterPattern(letter);
            
            if (pattern == null) return;
            
            int patternWidth = pattern.GetLength(0);
            int patternHeight = pattern.GetLength(1);
            
            for (int px = 0; px < patternWidth; px++)
            {
                for (int py = 0; py < patternHeight; py++)
                {
                    if (pattern[px, py])
                    {
                        // Calculate pixel position on texture
                        int startX = margin + (px * letterWidth / patternWidth);
                        int startY = margin + (py * letterHeight / patternHeight);
                        int endX = margin + ((px + 1) * letterWidth / patternWidth);
                        int endY = margin + ((py + 1) * letterHeight / patternHeight);
                        
                        // Fill rectangle
                        for (int x = startX; x < endX; x++)
                        {
                            for (int y = startY; y < endY; y++)
                            {
                                if (x >= 0 && x < size && y >= 0 && y < size)
                                {
                                    texture.SetPixel(x, y, textColor);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private bool[,] GetLetterPattern(char letter)
        {
            // 5x7 pixel patterns for letters (simplified bitmap font)
            switch (char.ToUpper(letter))
            {
                case 'A': return new bool[,] {
                    {false, true, true, true, false},
                    {true, false, false, false, true},
                    {true, false, false, false, true},
                    {true, true, true, true, true},
                    {true, false, false, false, true},
                    {true, false, false, false, true},
                    {true, false, false, false, true}
                };
                case 'E': return new bool[,] {
                    {true, true, true, true, true},
                    {true, false, false, false, false},
                    {true, false, false, false, false},
                    {true, true, true, true, false},
                    {true, false, false, false, false},
                    {true, false, false, false, false},
                    {true, true, true, true, true}
                };
                case 'H': return new bool[,] {
                    {true, false, false, false, true},
                    {true, false, false, false, true},
                    {true, false, false, false, true},
                    {true, true, true, true, true},
                    {true, false, false, false, true},
                    {true, false, false, false, true},
                    {true, false, false, false, true}
                };
                case 'N': return new bool[,] {
                    {true, false, false, false, true},
                    {true, true, false, false, true},
                    {true, false, true, false, true},
                    {true, false, false, true, true},
                    {true, false, false, false, true},
                    {true, false, false, false, true},
                    {true, false, false, false, true}
                };
                case 'O': return new bool[,] {
                    {false, true, true, true, false},
                    {true, false, false, false, true},
                    {true, false, false, false, true},
                    {true, false, false, false, true},
                    {true, false, false, false, true},
                    {true, false, false, false, true},
                    {false, true, true, true, false}
                };
                case 'P': return new bool[,] {
                    {true, true, true, true, false},
                    {true, false, false, false, true},
                    {true, false, false, false, true},
                    {true, true, true, true, false},
                    {true, false, false, false, false},
                    {true, false, false, false, false},
                    {true, false, false, false, false}
                };
                case 'R': return new bool[,] {
                    {true, true, true, true, false},
                    {true, false, false, false, true},
                    {true, false, false, false, true},
                    {true, true, true, true, false},
                    {true, false, true, false, false},
                    {true, false, false, true, false},
                    {true, false, false, false, true}
                };
                case 'S': return new bool[,] {
                    {false, true, true, true, true},
                    {true, false, false, false, false},
                    {true, false, false, false, false},
                    {false, true, true, true, false},
                    {false, false, false, false, true},
                    {false, false, false, false, true},
                    {true, true, true, true, false}
                };
                case 'T': return new bool[,] {
                    {true, true, true, true, true},
                    {false, false, true, false, false},
                    {false, false, true, false, false},
                    {false, false, true, false, false},
                    {false, false, true, false, false},
                    {false, false, true, false, false},
                    {false, false, true, false, false}
                };
                case 'M': return new bool[,] {
                    {true, false, false, false, true},
                    {true, true, false, true, true},
                    {true, false, true, false, true},
                    {true, false, false, false, true},
                    {true, false, false, false, true},
                    {true, false, false, false, true},
                    {true, false, false, false, true}
                };
                default:
                    // Default block pattern for unknown letters
                    return new bool[,] {
                        {true, true, true, true, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, false, false, false, true},
                        {true, true, true, true, true}
                    };
            }
        }
        
        private void OnDestroy()
        {
            // Clean up textures
            foreach (var tex in letterTextures.Values)
            {
                if (tex != null) Destroy(tex);
            }
            letterTextures.Clear();
            
            // Clean up materials
            foreach (var mat in letterMaterials.Values)
            {
                if (mat != null) Destroy(mat);
            }
            letterMaterials.Clear();
        }
    }
}
