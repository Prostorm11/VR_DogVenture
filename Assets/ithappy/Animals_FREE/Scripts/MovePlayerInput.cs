using UnityEngine;

namespace ithappy.Animals_FREE
{
    [RequireComponent(typeof(CreatureMover))]
    public class MovePlayerInput : MonoBehaviour
    {
        [Header("Character")]
        [SerializeField] private string m_HorizontalAxis = "Horizontal";
        [SerializeField] private string m_VerticalAxis = "Vertical";
        [SerializeField] private KeyCode m_RunKey = KeyCode.LeftShift;

        [Header("Camera")]
        [SerializeField] private PlayerCamera m_Camera;
        [SerializeField] private string m_MouseX = "Mouse X";
        [SerializeField] private string m_MouseY = "Mouse Y";
        [SerializeField] private string m_MouseScroll = "Mouse ScrollWheel";

        private CreatureMover m_Mover;

        private Vector2 m_Axis;
        private bool m_IsRun;

        private Vector3 m_Target;
        private Vector2 m_MouseDelta;
        private float m_Scroll;

        private void Awake()
        {
            // Get the CreatureMover component on this GameObject
            m_Mover = GetComponent<CreatureMover>();
        }

        private void Update()
        {
            GatherInput();
            ApplyInput();
        }

        /// <summary>
        /// Reads keyboard/mouse input and camera target.
        /// </summary>
        private void GatherInput()
        {
            // Movement input
            m_Axis = new Vector2(Input.GetAxis(m_HorizontalAxis), Input.GetAxis(m_VerticalAxis));
            m_IsRun = Input.GetKey(m_RunKey);

            // Camera input
            m_Target = (m_Camera == null) ? Vector3.zero : m_Camera.Target;
            m_MouseDelta = new Vector2(Input.GetAxis(m_MouseX), Input.GetAxis(m_MouseY));
            m_Scroll = Input.GetAxis(m_MouseScroll);
        }

        /// <summary>
        /// Sends gathered input to CreatureMover and PlayerCamera.
        /// </summary>
        private void ApplyInput()
        {
            if (m_Mover != null)
            {
                // Feed movement input to CreatureMover
                // The player never moves backward like the dog, so 'moveBackward' is false
                m_Mover.SetCommand(m_Axis, m_Target, m_IsRun, true);

            }

            if (m_Camera != null)
            {
                // Feed mouse input to PlayerCamera
                m_Camera.SetInput(m_MouseDelta, m_Scroll);
            }
        }

        /// <summary>
        /// Optional: Bind a different CreatureMover at runtime.
        /// </summary>
        public void BindMover(CreatureMover mover)
        {
            m_Mover = mover;
        }
    }
}
