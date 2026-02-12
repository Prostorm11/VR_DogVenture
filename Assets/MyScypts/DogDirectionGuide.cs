using UnityEngine;

public class DogDirectionGuide : MonoBehaviour
{
    [Header("Target (Waypoint / Arrow / Destination)")]
    public Transform target;

    [Header("Rotation")]
    public float rotationSpeed = 5f;

    [Header("Animation")]
    public Animator animator;
    public string speedParameter = "Speed";

    void Update()
    {
        if (target == null) return;

        // Direction from user (camera) to target
        Vector3 direction = target.position - transform.position;

        // Ignore vertical difference
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            animator.SetFloat(speedParameter, 0f);
            return;
        }

        // Desired rotation
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Smooth rotation
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        // Animate "guiding" motion
        animator.SetFloat(speedParameter, 1f);
    }
}
