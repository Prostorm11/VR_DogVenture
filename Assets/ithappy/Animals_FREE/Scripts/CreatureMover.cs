using System;
using UnityEngine;

namespace ithappy.Animals_FREE
{
    [RequireComponent(typeof(Animator))]
    public class CreatureMover : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 1.5f;
        public float runSpeed = 3f;
        public float rotationSpeed = 6f;

        [Header("Animator")]
        [SerializeField] private string verticalID = "Vert";
        [SerializeField] private string stateID = "State";
        [SerializeField]
        private LookWeight lookWeight = new LookWeight(1f, 0.3f, 0.7f, 1f);

        private Animator animator;
        private Transform tr;

        private Vector2 axis;
        private Vector3 target;
        private bool isRun;
        private bool shouldMove;

        private Vector2 smoothAxis;
        private float smoothState;

        [Header("VR Animation Settings")]
        [Tooltip("Multiplier to make legs move faster than world movement in VR.")]
        public float animSpeedMultiplier = 2f;
        [Tooltip("Minimum leg movement value to avoid tiptoe effect in VR.")]
        public float minWalkState = 0.3f;

        private void Awake()
        {
            tr = transform;
            animator = GetComponent<Animator>();
        }

        private void Update()
        {
            Animate();
            RotateTowardsTarget();

            if (!shouldMove) return;

            float speed = isRun ? runSpeed : walkSpeed;
            Vector3 move = tr.forward * axis.y * speed * Time.deltaTime;
            tr.position += move;
        }

        private void Animate()
        {
            smoothAxis = Vector2.Lerp(smoothAxis, axis, 4.5f * Time.deltaTime);

            // VR-friendly smoothing with minimum walk state
            smoothState = Mathf.Lerp(smoothState, isRun ? 1f : minWalkState, 4.5f * Time.deltaTime);

            // Multiply vertical movement for slow VR speeds
            animator.SetFloat(verticalID, smoothAxis.magnitude * animSpeedMultiplier);
            animator.SetFloat(stateID, smoothState);
        }

        private void RotateTowardsTarget()
        {
            Vector3 dir = target - tr.position;
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.0001f) return;

            Quaternion rot = Quaternion.LookRotation(dir);
            tr.rotation = Quaternion.Slerp(tr.rotation, rot, rotationSpeed * Time.deltaTime);
        }

        private void OnAnimatorIK()
        {
            animator.SetLookAtPosition(target);
            animator.SetLookAtWeight(
                lookWeight.weight,
                lookWeight.body,
                lookWeight.head,
                lookWeight.eyes
            );
        }

        // SINGLE ENTRY POINT
        public void SetCommand(Vector2 axis, Vector3 target, bool run, bool move)
        {
            this.axis = Vector2.ClampMagnitude(axis, 1f);
            this.target = target;
            this.isRun = run;
            this.shouldMove = move;
        }

        [Serializable]
        private struct LookWeight
        {
            public float weight, body, head, eyes;
            public LookWeight(float w, float b, float h, float e)
            {
                weight = w;
                body = b;
                head = h;
                eyes = e;
            }
        }
    }
}
