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

    [Header("Hit Flash Feedback")]
    public Color hitFlashColor = Color.white;
    public float flashDuration = 0.08f;
    private SkinnedMeshRenderer[] meshRenderers;
    private List<Color> originalColors = new List<Color>();
    private Coroutine flashRoutine;

    // Component references
    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;
    private List<BulletMark> attachedBulletMarks = new List<BulletMark>();

    // Shader property ID for performance
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP default; use "_Color" for Built-in RP

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Cache all renderers on this model (including child bones)
        meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var r in meshRenderers)
        {
            if (r.material.HasProperty(BaseColorId))
                originalColors.Add(r.material.GetColor(BaseColorId));
            else if (r.material.HasProperty("_Color"))
                originalColors.Add(r.material.GetColor("_Color"));
            else
                originalColors.Add(Color.white);
        }

        FindPlayer();
    }

    void OnEnable()
    {
        currentHealth = maxHealth;
        lastAttackTime = -attackCooldown;
        ResetRendererColors();
        FindPlayer();
    }

    void OnDisable()
    {
        DeactivateAllBulletMarks();
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
        ResetRendererColors();
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

        // Delay the actual damage to match the animation frame
        StartCoroutine(DealAttackDamageDelayed(attackImpactDelay));
    }

    IEnumerator DealAttackDamageDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Ensure enemy hasn't died during the windup swing
        if (currentHealth > 0 && player != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            // Verify player is still in melee distance
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
        animator.SetTrigger("Hit");

        // Set all mesh parts to flash color
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (meshRenderers[i] != null)
            {
                if (meshRenderers[i].material.HasProperty(BaseColorId))
                    meshRenderers[i].material.SetColor(BaseColorId, hitFlashColor);
                else if (meshRenderers[i].material.HasProperty("_Color"))
                    meshRenderers[i].material.SetColor("_Color", hitFlashColor);
            }
        }

        yield return new WaitForSeconds(flashDuration);

        ResetRendererColors();
        flashRoutine = null;
    }

    void ResetRendererColors()
    {
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (meshRenderers[i] != null && i < originalColors.Count)
            {
                if (meshRenderers[i].material.HasProperty(BaseColorId))
                    meshRenderers[i].material.SetColor(BaseColorId, originalColors[i]);
                else if (meshRenderers[i].material.HasProperty("_Color"))
                    meshRenderers[i].material.SetColor("_Color", originalColors[i]);
            }
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