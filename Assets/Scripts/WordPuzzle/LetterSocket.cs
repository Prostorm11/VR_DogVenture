using UnityEngine;

namespace VRProject.WordPuzzle
{
    /// <summary>
    /// A socket that letters can snap into.
    /// When a letter is dropped near a socket, it snaps in place.
    /// </summary>
    public class LetterSocket : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float snapRadius = 0.15f; // Distance for snapping
        [SerializeField] private Color emptyColor = new Color(0.2f, 0.7f, 1f, 0.5f);
        [SerializeField] private Color filledColor = new Color(0.2f, 1f, 0.5f, 0.7f);
        [SerializeField] private Color correctColor = new Color(0.2f, 1f, 0.3f, 0.9f);
        [SerializeField] private Color incorrectColor = new Color(1f, 0.3f, 0.2f, 0.9f);

        private int index;
        private SubWordGameManager gameManager;
        private FloatingLetter currentLetter;
        private Renderer socketRenderer;

        public int Index => index;
        public bool HasLetter => currentLetter != null;
        public FloatingLetter CurrentLetter => currentLetter;

        public void Initialize(int socketIndex, SubWordGameManager manager)
        {
            index = socketIndex;
            gameManager = manager;

            // Get renderer from child cube
            socketRenderer = GetComponentInChildren<Renderer>();
            SetColor(emptyColor);
        }

        /// <summary>
        /// Try to place a letter in this socket.
        /// </summary>
        public bool TryPlaceLetter(FloatingLetter letter)
        {
            if (currentLetter != null)
            {
                Debug.Log($"[Socket {index}] Already has a letter");
                return false;
            }

            float distance = Vector3.Distance(letter.transform.position, transform.position);
            if (distance > snapRadius)
            {
                return false;
            }

            // Snap letter to socket
            currentLetter = letter;
            letter.SnapToSocket(this);
            
            SetColor(filledColor);
            
            // Notify manager
            if (gameManager != null)
            {
                gameManager.OnLetterPlacedInSocket(this, letter);
            }

            Debug.Log($"[Socket {index}] Letter '{letter.Letter}' placed");
            return true;
        }

        /// <summary>
        /// Remove the current letter from this socket.
        /// </summary>
        public void ClearLetter()
        {
            if (currentLetter != null)
            {
                currentLetter.ClearFromSocket();
                currentLetter = null;
            }
            SetColor(emptyColor);
        }

        /// <summary>
        /// Called when letter is grabbed out of socket.
        /// </summary>
        public void OnLetterRemoved()
        {
            currentLetter = null;
            SetColor(emptyColor);

            if (gameManager != null)
            {
                gameManager.OnLetterRemovedFromSocket(this);
            }
        }

        public void ShowCorrect()
        {
            SetColor(correctColor);
        }

        public void ShowIncorrect()
        {
            SetColor(incorrectColor);
        }

        private void SetColor(Color color)
        {
            if (socketRenderer != null)
            {
                socketRenderer.material.color = color;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if a letter entered
            FloatingLetter letter = other.GetComponent<FloatingLetter>();
            if (letter != null && !letter.IsBeingHeld && currentLetter == null)
            {
                TryPlaceLetter(letter);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = HasLetter ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, snapRadius);
        }
    }
}
