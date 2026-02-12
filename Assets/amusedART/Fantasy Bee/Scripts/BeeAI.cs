using UnityEngine;

public class BeeAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f; // Increased for more visible movement
    public float rotationSpeed = 5f;
    public float changeDirectionTime = 2f; // Change direction more frequently

    [Header("Boundaries")]
    public Vector3 arenaCenter = Vector3.zero;
    public float arenaSize = 8f; // Patrol in 16x16 area
    public float flyHeight = 2f;

    [Header("Detection")]
    public float detectionRange = 8f;
    public float attackRange = 2f;
    public float attackCooldown = 2f;

    [Header("Health")]
    public int maxHealth = 100;

    private Animator animator;
    private int currentHealth;
    private bool isDead = false;

    // Simple random walk
    private Vector3 randomDirection;
    private float directionTimer = 0f;

    // Player tracking
    private Transform player;
    private float lastAttackTime = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;

        // Start at arena center at fly height
        Vector3 startPos = arenaCenter;
        startPos.y = flyHeight;
        transform.position = startPos;

        // Pick initial random direction
        ChangeRandomDirection();

        Debug.Log("Bee AI Started! Position: " + transform.position);
        Debug.Log("Moving in direction: " + randomDirection);
    }

    void Update()
    {
        if (isDead) return;

        // FORCE bee to stay within boundaries
        EnforceBoundaries();

        // Find player if not found yet
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("Player found!");
            }
        }

        // If player exists and is close, interact with them
        if (player != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, player.position);

            if (distToPlayer <= attackRange)
            {
                AttackPlayer();
                return;
            }
            else if (distToPlayer <= detectionRange)
            {
                ChasePlayer();
                return;
            }
        }

        // No player nearby - random patrol (THIS IS WHAT SHOULD BE RUNNING)
        RandomPatrol();
    }

    void EnforceBoundaries()
    {
        Vector3 pos = transform.position;
        bool hitBoundary = false;

        // Keep within X boundaries
        if (pos.x > arenaCenter.x + arenaSize)
        {
            pos.x = arenaCenter.x + arenaSize;
            randomDirection.x = -Mathf.Abs(randomDirection.x); // Bounce
            hitBoundary = true;
        }
        else if (pos.x < arenaCenter.x - arenaSize)
        {
            pos.x = arenaCenter.x - arenaSize;
            randomDirection.x = Mathf.Abs(randomDirection.x); // Bounce
            hitBoundary = true;
        }

        // Keep within Z boundaries
        if (pos.z > arenaCenter.z + arenaSize)
        {
            pos.z = arenaCenter.z + arenaSize;
            randomDirection.z = -Mathf.Abs(randomDirection.z); // Bounce
            hitBoundary = true;
        }
        else if (pos.z < arenaCenter.z - arenaSize)
        {
            pos.z = arenaCenter.z - arenaSize;
            randomDirection.z = Mathf.Abs(randomDirection.z); // Bounce
            hitBoundary = true;
        }

        // Always stay at fly height
        pos.y = flyHeight;

        transform.position = pos;

        if (hitBoundary)
        {
            Debug.Log("Hit boundary! New direction: " + randomDirection);
        }
    }

    void RandomPatrol()
    {
        // Timer to change direction
        directionTimer += Time.deltaTime;
        if (directionTimer >= changeDirectionTime)
        {
            ChangeRandomDirection();
            directionTimer = 0f;
        }

        // Face the direction we're moving
        if (randomDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(randomDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // MOVE THE BEE!
        Vector3 movement = randomDirection * moveSpeed * Time.deltaTime;
        transform.position += movement;

        // Play move animation
        if (animator != null)
        {
            animator.SetTrigger("Move");
        }

        // Debug - show we're moving
        Debug.DrawRay(transform.position, randomDirection * 2f, Color.green);
    }

    void ChangeRandomDirection()
    {
        // Pick random angle
        float angle = Random.Range(0f, 360f);

        // Convert to direction (only horizontal - no Y change)
        randomDirection = new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            0f,
            Mathf.Sin(angle * Mathf.Deg2Rad)
        ).normalized;

        Debug.Log("Changed direction to: " + randomDirection + " (angle: " + angle + ")");
    }

    void ChasePlayer()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        transform.position += dirToPlayer * moveSpeed * 1.5f * Time.deltaTime;

        if (animator != null)
        {
            animator.SetTrigger("Move");
        }

        Debug.Log("Chasing player!");
    }

    void AttackPlayer()
    {
        // Face player
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // Attack with cooldown
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
            lastAttackTime = Time.time;
            Debug.Log("BEE ATTACKS!");
        }
        else
        {
            if (animator != null)
            {
                animator.SetTrigger("Idle");
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (animator != null)
        {
            animator.SetTrigger("Damage");
        }

        Debug.Log("Bee took " + damage + " damage! Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        // Disable colliders
        Collider[] cols = GetComponents<Collider>();
        foreach (Collider c in cols)
        {
            c.enabled = false;
        }

        Debug.Log("Bee died!");

        // Destroy after 3 seconds
        Destroy(gameObject, 3f);
    }

    // Draw the patrol area in the editor
    void OnDrawGizmos()
    {
        // Draw the arena boundaries (always visible)
        Gizmos.color = Color.cyan;

        Vector3 bottomLeft = arenaCenter + new Vector3(-arenaSize, 0.1f, -arenaSize);
        Vector3 bottomRight = arenaCenter + new Vector3(arenaSize, 0.1f, -arenaSize);
        Vector3 topLeft = arenaCenter + new Vector3(-arenaSize, 0.1f, arenaSize);
        Vector3 topRight = arenaCenter + new Vector3(arenaSize, 0.1f, arenaSize);

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);

        // Draw center point
        Gizmos.color = Color.yellow;
        Vector3 centerPoint = arenaCenter;
        centerPoint.y = Application.isPlaying ? flyHeight : 2f;
        Gizmos.DrawSphere(centerPoint, 0.3f);

        // During play, show current direction
        if (Application.isPlaying && !isDead)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, randomDirection * 3f);
        }
    }
}