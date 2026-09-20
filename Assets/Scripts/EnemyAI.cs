using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Convert layer name to layer index
        int playerLayer = LayerMask.NameToLayer("PlayerRoot");

        // Search active scene objects to find the one matching the PlayerRoot layer
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == playerLayer)
            {
                player = obj.transform;
                break;
            }
        }

    if (player == null)
    {
        Debug.LogWarning("EnemyAI: Could not find any GameObject on the 'PlayerRoot' layer!");
    }
}

    void OnEnable()
    {
        // Reset health every time it spawns
        currentHealth = maxHealth;
        FindPlayer();
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
                break;
            }
        }
    }

    void Update()
    {
        if (player != null && currentHealth > 0 && agent.isOnNavMesh)
        {
            agent.SetDestination(player.position);
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    // Call this function from your Raycast shooting script when the gun hits this enemy
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Tell the GameManager we killed one and get points
        GameManager.Instance.AddScore(10);

        // Instead of Destroy(gameObject), we deactivate it to return it to the pool
        gameObject.SetActive(false);
    }
}