using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    private int currentHealth;
    private bool isDead = false;

    [Header("Attack Timing")]
    public float attackRange = 2.0f;
    public float attackCooldown = 1.6f;
    public float attackDamage = 15f;
    public float attackImpactDelay = 1f;
    private float lastAttackTime;

    [Header("Hit Visuals & FX")]
    [Tooltip("List of hit effects (blood splatters, streams, sparks) randomly chosen on hit")]
    public GameObject[] bloodEffectPrefabs;
    public float corpseDespawnDelay = 3.5f;

    [Header("Hit Flash Setup")]
    public SkinnedMeshRenderer targetRenderer;
    public Material normalMaterial;
    public Material hitFlashMaterial;
    public float flashDuration = 0.08f;
    private Coroutine flashRoutine;

    // Component references
    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;
    private Collider enemyCollider;
    private List<BulletMark> attachedBulletMarks = new List<BulletMark>();

    [Header("Power-Up Visuals & SO States")]
    public BoolVariable gogglesState;
    public BoolVariable weakZoneState;
    public GameObject gogglesIndicator;
    public Collider[] possibleWeakZones;
    public GameObject[] weakZoneVisuals;
    private Collider activeWeakZoneCollider;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyCollider = GetComponent<Collider>();

        if (targetRenderer != null && normalMaterial == null)
        {
            normalMaterial = targetRenderer.sharedMaterial;
        }

        FindPlayer();
    }

    void OnEnable()
    {
        isDead = false;
        currentHealth = maxHealth;
        lastAttackTime = -attackCooldown;

        if (agent != null) agent.enabled = true;
        if (enemyCollider != null) enemyCollider.enabled = true;

        ResetToNormalMaterial();
        FindPlayer();

        // Subscribe to ScriptableObject events
        if (gogglesState != null) gogglesState.OnValueChanged += OnGogglesStateChanged;
        if (weakZoneState != null) weakZoneState.OnValueChanged += OnWeakZoneStateChanged;

        activeWeakZoneCollider = null;
        RefreshPowerUpVisuals();
    }

    void OnDisable()
    {
        // Unsubscribe from ScriptableObject events
        if (gogglesState != null) gogglesState.OnValueChanged -= OnGogglesStateChanged;
        if (weakZoneState != null) weakZoneState.OnValueChanged -= OnWeakZoneStateChanged;

        DeactivateAllBulletMarks();
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        ResetToNormalMaterial();

        activeWeakZoneCollider = null;
        if (gogglesIndicator != null) gogglesIndicator.SetActive(false);

        for (int i = 0; i < weakZoneVisuals.Length; i++)
        {
            if (weakZoneVisuals[i] != null) weakZoneVisuals[i].SetActive(false);
        }
    }

    private void OnGogglesStateChanged(bool isActive) => RefreshPowerUpVisuals();
    private void OnWeakZoneStateChanged(bool isActive) => RefreshPowerUpVisuals();

    public void RefreshPowerUpVisuals()
    {
        bool isGogglesOn = gogglesState != null && gogglesState.Value;
        bool isWeakZoneOn = weakZoneState != null && weakZoneState.Value;

        if (gogglesIndicator != null)
        {
            gogglesIndicator.SetActive(isGogglesOn);
        }

        if (isWeakZoneOn)
        {
            if (activeWeakZoneCollider == null && possibleWeakZones != null && possibleWeakZones.Length > 0)
            {
                int randIndex = Random.Range(0, possibleWeakZones.Length);
                activeWeakZoneCollider = possibleWeakZones[randIndex];
            }

            for (int i = 0; i < weakZoneVisuals.Length; i++)
            {
                if (weakZoneVisuals[i] != null)
                {
                    bool isMatch = (activeWeakZoneCollider != null && i < possibleWeakZones.Length && possibleWeakZones[i] == activeWeakZoneCollider);
                    weakZoneVisuals[i].SetActive(isMatch);
                }
            }
        }
        else
        {
            activeWeakZoneCollider = null;
            for (int i = 0; i < weakZoneVisuals.Length; i++)
            {
                if (weakZoneVisuals[i] != null) weakZoneVisuals[i].SetActive(false);
            }
        }
    }

    public bool CheckIfWeakZone(Collider hitCollider)
    {
        bool isWeakZoneOn = weakZoneState != null && weakZoneState.Value;
        return isWeakZoneOn && hitCollider == activeWeakZoneCollider;
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
        if (isDead || player == null || currentHealth <= 0 || !agent.isOnNavMesh) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            agent.isStopped = true;
            animator.SetFloat("Speed", 0f);

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
        if (animator != null) animator.SetTrigger("Attack");
        StartCoroutine(DealAttackDamageDelayed(attackImpactDelay));
    }

    IEnumerator DealAttackDamageDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!isDead && currentHealth > 0 && player != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= attackRange + 0.6f)
            {
                if (GameManager.Instance != null) GameManager.Instance.TakePlayerDamage(attackDamage);
            }
        }
    }

    public void SpawnHitEffect(Vector3 spawnPosition, Quaternion spawnRotation, Transform parentTransform)
    {
        if (bloodEffectPrefabs != null && bloodEffectPrefabs.Length > 0)
        {
            int randIdx = Random.Range(0, bloodEffectPrefabs.Length);
            if (bloodEffectPrefabs[randIdx] != null)
            {
                GameObject fx = Instantiate(bloodEffectPrefabs[randIdx], spawnPosition, spawnRotation, parentTransform);
                Destroy(fx, 2.5f);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(DamageFlashRoutine());

        if (currentHealth <= 0)
        {
            StartCoroutine(DieSequence());
        }
    }

    IEnumerator DamageFlashRoutine()
    {
        if (animator != null && !isDead) animator.SetTrigger("Hit");

        if (targetRenderer != null && hitFlashMaterial != null)
        {
            Material[] flashMats = new Material[targetRenderer.sharedMaterials.Length];
            for (int i = 0; i < flashMats.Length; i++) flashMats[i] = hitFlashMaterial;
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
            for (int i = 0; i < normalMats.Length; i++) normalMats[i] = normalMaterial;
            targetRenderer.materials = normalMats;
        }
    }

    public void RegisterBulletMark(BulletMark mark)
    {
        if (!attachedBulletMarks.Contains(mark)) attachedBulletMarks.Add(mark);
    }

    public void DeactivateAllBulletMarks()
    {
        for (int i = 0; i < attachedBulletMarks.Count; i++)
        {
            if (attachedBulletMarks[i] != null) attachedBulletMarks[i].DeactivateMark();
        }
        attachedBulletMarks.Clear();
    }

    IEnumerator DieSequence()
    {
        isDead = true;

        if (agent != null) agent.enabled = false;
        if (enemyCollider != null) enemyCollider.enabled = false;

        if (animator != null) animator.SetTrigger("Dead");

        DeactivateAllBulletMarks();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(10);
        }

        yield return new WaitForSeconds(corpseDespawnDelay);

        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        DeactivateAllBulletMarks();
    }
}