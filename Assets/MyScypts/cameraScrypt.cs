using UnityEngine;

public class cameraScrypt : MonoBehaviour
{
    [Header("Target")]
    public Transform target;          // Drag the Dog here

    [Header("Camera Settings")]
    public float distance = 4f;
    public float height = 1.5f;
    public float rotationSpeed = 4f;
    public float minPitch = 5f;
    public float maxPitch = 40f;

    private float yaw;
    private float pitch = 15f;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("myCameraScrypt: Target not assigned!");
            enabled = false;
            return;
        }

        // Start camera aligned with target direction
        yaw = target.eulerAngles.y;
    }

    void LateUpdate()
    {
        // Mouse input
        yaw += Input.GetAxis("Mouse X") * rotationSpeed;
        pitch -= Input.GetAxis("Mouse Y") * rotationSpeed;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Rotation
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Offset calculation
        Vector3 offset = rotation * new Vector3(0, 0, -distance);

        // Final camera position
        transform.position = target.position + Vector3.up * height + offset;

        // Always look at the target
        transform.LookAt(target.position + Vector3.up * height);
    }
}
