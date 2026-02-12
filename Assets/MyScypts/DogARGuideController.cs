using ithappy.Animals_FREE;
using UnityEngine;

public class DogARGuideController : MonoBehaviour
{
    [Header("References")]
    public Transform player;           // Player to follow
    public CreatureMover mover;        // CreatureMover controlling the dog

    [Header("Distance Settings")]
    public float minDistance = 2f;      // Minimum distance before dog backs off
    public float preferredDistance = 4f;// Comfortable guiding distance
    public float maxDistance = 8f;      // Distance beyond which dog runs to catch up
    public float stopThreshold = 0.3f;  // Stops moving if within this range

    [Header("Movement Settings")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 3f;
    public float rotationSpeed = 5f;

    [Header("Side Offset")]
    public float sideOffset = 0.5f;     // Slight offset to avoid blocking player

    private void Update()
    {
        if (!player || !mover) return;

        // 1️⃣ Calculate the target position in front of the player
        Vector3 forwardOffset = player.forward * preferredDistance;
        Vector3 rightOffset = player.right * sideOffset;
        Vector3 targetPos = player.position + forwardOffset + rightOffset;
        targetPos.y = transform.position.y; // Keep dog on same height

        // 2️⃣ Compute distance from dog to target
        float distance = Vector3.Distance(transform.position, targetPos);

        // 3️⃣ Decide movement
        bool move = false;
        bool run = false;
        Vector2 axis = Vector2.zero;

        if (distance <= stopThreshold)
        {
            // Close enough → stop moving
            move = false;
            axis = Vector2.zero;
        }
        else
        {
            // Move toward target
            Vector3 dir = (targetPos - transform.position).normalized;
            axis = ToLocalAxis(dir);

            move = true;

            // Run if too far, otherwise walk
            run = distance > preferredDistance;
        }

        // 4️⃣ Rotate dog smoothly toward target
        RotateTowards(targetPos);

        // 5️⃣ Send command to CreatureMover
        mover.SetCommand(axis, targetPos, run, move);
    }

    // Convert a world direction vector into local X/Z axis for CreatureMover
    private Vector2 ToLocalAxis(Vector3 worldDir)
    {
        Vector3 local = transform.InverseTransformDirection(worldDir);
        return new Vector2(local.x, local.z);
    }

    // Smoothly rotate dog to face the target position
    private void RotateTowards(Vector3 targetPos)
    {
        Vector3 lookDir = targetPos - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(lookDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }
}
