using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Attack Timing")]
    public float attackRange = 2.0f;
    public float attackCooldown = 1.6f;
    public float attackDamage = 15f;
    [Tooltip("Time from animation start until the hit lands (e.g. 0.4s into a punch)")]
    public float attackImpactDelay = 1f;
    private float lastAttackTime;

    [Header("Hit Flash Setup")]
    [Tooltip("Drag the specific SkinnedMeshRenderer of the enemy body here")]
    public SkinnedMeshRenderer targetRenderer;
    [Tooltip("Default material used by the enemy mesh")]
    public Material normalMaterial;
    [Tooltip("Material swapped in briefly when hit (e.g., solid white/unlit)")]
    public Material hitFlashMaterial;
    public float flashDuration = 0.08f;
    private Coroutine flashRoutine;

    // Component references
    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;
    private List<BulletMark> attachedBulletMarks = new List<BulletMark>();

    [Header("Power-Up Visuals")]
    [Tooltip("The glowing cube above the enemy for Goggles")]
    public GameObject gogglesIndicator; 
    
    [Tooltip("List of colliders that can become weak zones (e.g., Head, Arm, Leg)")]
    public Collider[] possibleWeakZones;
    [Tooltip("Corresponding visual cues for the weak zones (must match index order above)")]
    public GameObject[] weakZoneVisuals;
    
    private Collider activeWeakZoneCollider;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Fallback: If you forgot to drag normalMaterial, grab the renderer's current one
        if (targetRenderer != null && normalMaterial == null)
        {
            normalMaterial = targetRenderer.sharedMaterial;
        }

        FindPlayer();
    }

    void OnEnable()
    {
        currentHealth = maxHealth;
        lastAttackTime = -attackCooldown;
        ResetToNormalMaterial();
        FindPlayer();

        activeWeakZoneCollider = null;
        RefreshPowerUpVisuals();
    }

    public void RefreshPowerUpVisuals()
    {
        if (GameManager.Instance == null) return;

        // 1. Goggles Power-Up Indicator
        if (gogglesIndicator != null)
        {
            gogglesIndicator.SetActive(GameManager.Instance.isGogglesActive);
        }

        // 2. Weak Zone Power-Up
        if (GameManager.Instance.isWeakZoneActive)
        {
            // Pick a random weak zone collider if none is currently active
            if (activeWeakZoneCollider == null && possibleWeakZones != null && possibleWeakZones.Length > 0)
            {
                int randIndex = Random.Range(0, possibleWeakZones.Length);
                activeWeakZoneCollider = possibleWeakZones[randIndex];
            }

            // Enable ONLY the active weak zone visual GameObject
            for (int i = 0; i < weakZoneVisuals.Length; i++)
            {
                if (weakZoneVisuals[i] != null)
                {
                    bool isMatch = false;
                    if (activeWeakZoneCollider != null && i < possibleWeakZones.Length)
                    {
                        isMatch = (possibleWeakZones[i] == activeWeakZoneCollider);
                    }

                    // Directly enable/disable the GameObject itself!
                    weakZoneVisuals[i].SetActive(isMatch);
                }
            }
        }
        else
        {
            // Power-up expired: reset state and turn OFF all visual GameObjects
            activeWeakZoneCollider = null;
            for (int i = 0; i < weakZoneVisuals.Length; i++)
            {
                if (weakZoneVisuals[i] != null)
                {
                    weakZoneVisuals[i].SetActive(false);
                }
            }
        }
    }

    void OnDisable()
    {
        DeactivateAllBulletMarks();
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
        ResetToNormalMaterial();

        // Reset Weak Zone states
        activeWeakZoneCollider = null;
        if (gogglesIndicator != null) gogglesIndicator.SetActive(false);

        // Hide all visual GameObjects when disabled
        for (int i = 0; i < weakZoneVisuals.Length; i++)
        {
            if (weakZoneVisuals[i] != null)
            {
                weakZoneVisuals[i].SetActive(false);
            }
        }
    }

    public bool CheckIfWeakZone(Collider hitCollider)
    {
        return GameManager.Instance != null && GameManager.Instance.isWeakZoneActive && hitCollider == activeWeakZoneCollider;
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
        if (player == null || currentHealth <= 0 || !agent.isOnNavMesh) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            agent.isStopped = true;
            animator.SetFloat("Speed", 0f);

            // Turn towards the player
            Vector3 lookDir = (player.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
            }

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                AttackPlayer();
            }
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    void AttackPlayer()
    {
        lastAttackTime = Time.time;

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        StartCoroutine(DealAttackDamageDelayed(attackImpactDelay));
    }

    IEnumerator DealAttackDamageDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (currentHealth > 0 && player != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= attackRange + 0.6f)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TakePlayerDamage(attackDamage);
                }
            }
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // Visual damage flash
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(DamageFlashRoutine());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator DamageFlashRoutine()
    {
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }

        if (targetRenderer != null && hitFlashMaterial != null)
        {
            // Replace all submesh material slots with the flash material
            Material[] flashMats = new Material[targetRenderer.sharedMaterials.Length];
            for (int i = 0; i < flashMats.Length; i++)
            {
                flashMats[i] = hitFlashMaterial;
            }
            targetRenderer.materials = flashMats;
        }

        yield return new WaitForSeconds(flashDuration);

        ResetToNormalMaterial();
        flashRoutine = null;
    }

    void ResetToNormalMaterial()
    {
        if (targetRenderer != null && normalMaterial != null)
        {
            Material[] normalMats = new Material[targetRenderer.sharedMaterials.Length];
            for (int i = 0; i < normalMats.Length; i++)
            {
                normalMats[i] = normalMaterial;
            }
            targetRenderer.materials = normalMats;
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

    void OnDestroy()
    {
        DeactivateAllBulletMarks();
    }
}