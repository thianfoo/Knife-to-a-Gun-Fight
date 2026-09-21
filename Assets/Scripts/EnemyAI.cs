using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;

    // Stores references to any bullet marks stuck to this enemy
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
        FindPlayer();
    }

    void OnDisable()
    {
        // Deactivate all marks if the enemy is disabled
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
                break;
            }
        }

        if (player == null)
        {
            Debug.LogWarning("EnemyAI: Could not find any GameObject on the 'PlayerRoot' layer!");
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

    // Called by BulletMark upon impact
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Called by BulletMark to register itself
    public void RegisterBulletMark(BulletMark mark)
    {
        if (!attachedBulletMarks.Contains(mark))
        {
            attachedBulletMarks.Add(mark);
        }
    }

    // Cleans up all decals stuck on this body
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
        // 1. Clear marks so they don't float in mid-air or stay when recycled
        DeactivateAllBulletMarks();

        // 2. Add score to the GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(10);
        }

        // 3. Return to object pool
        gameObject.SetActive(false);
    }
}