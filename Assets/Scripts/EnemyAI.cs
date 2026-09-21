using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MarsFPSKit; // Needed to interact with Kit_PlayerBehaviour

public class EnemyAI : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Attack Settings")]
    public float attackRange = 2.0f;
    public float attackCooldown = 1.5f;
    public float attackDamage = 15f;
    private float lastAttackTime;

    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;
    private Kit_PlayerBehaviour playerBehaviour;

    private List<BulletMark> attachedBulletMarks = new List<BulletMark>();

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        FindPlayer();
    }

    void OnEnable()
    {
        currentHealth = maxHealth;
        lastAttackTime = -attackCooldown; // Allow immediate attack if in range
        FindPlayer();
    }

    void OnDisable()
    {
        DeactivateAllBulletMarks();
    }

    void FindPlayer()
    {
        int playerLayer = LayerMask.NameToLayer("PlayerRoot");
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == playerLayer)
            {
                player = obj.transform;
                playerBehaviour = obj.GetComponent<Kit_PlayerBehaviour>();
                break;
            }
        }
    }

    void Update()
    {
        if (player == null || currentHealth <= 0 || !agent.isOnNavMesh) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            // Stop moving to execute attack
            agent.isStopped = true;
            animator.SetFloat("Speed", 0f);

            // Smoothly face the player
            Vector3 lookDirection = (player.position - transform.position).normalized;
            lookDirection.y = 0; // Keep rotation horizontal
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), Time.deltaTime * 10f);
            }

            // Check attack cooldown
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                AttackPlayer();
            }
        }
        else
        {
            // Resume chasing
            agent.isStopped = false;
            agent.SetDestination(player.position);
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    void AttackPlayer()
    {
        lastAttackTime = Time.time;

        // Trigger the attack animation in the Animator
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Deal damage to the player
        if (playerBehaviour != null)
        {
            playerBehaviour.ServerDamage(
                attackDamage,       // e.g., 25f
                -1,                 // Gun ID
                transform.position, // shotFrom
                transform.forward,  // direction
                100f,               // ragdoll force
                player.position,    // hit position
                0,                  // ragdoll body part ID
                true,               // isBot
                999                 // shooter ID
            );
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void RegisterBulletMark(BulletMark mark)
    {
        if (!attachedBulletMarks.Contains(mark))
        {
            attachedBulletMarks.Add(mark);
        }
    }

    public void DeactivateAllBulletMarks()
    {
        for (int i = 0; i < attachedBulletMarks.Count; i++)
        {
            if (attachedBulletMarks[i] != null)
            {
                attachedBulletMarks[i].DeactivateMark();
            }
        }
        attachedBulletMarks.Clear();
    }

    void Die()
    {
        DeactivateAllBulletMarks();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(10);
        }

        gameObject.SetActive(false);
    }
}