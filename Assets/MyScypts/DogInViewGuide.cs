using UnityEngine;

public class DogInViewGuide : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;

    [Header("Offsets")]
    public float forwardDistance = 1.5f;   // how far ahead
    public float sideDistance = 0.6f;       // left/right slide
    public float height = -0.3f;             // relative to camera

    [Header("Motion")]
    public float followSpeed = 6f;
    public float rotationSpeed = 8f;

    private Vector3 desiredPosition;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        // Direction camera is facing
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // Remove vertical tilt influence
        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        // Dog floats ahead & slightly toward look direction
        desiredPosition =
            cameraTransform.position +
            forward * forwardDistance +
            right * Mathf.Clamp(Input.GetAxis("Mouse X"), -1f, 1f) * sideDistance +
            Vector3.up * height;

        // Smooth movement
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        // Dog looks where the camera looks
        Quaternion lookRot = Quaternion.LookRotation(forward);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            lookRot,
            rotationSpeed * Time.deltaTime
        );
    }
}
