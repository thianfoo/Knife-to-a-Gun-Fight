using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossAI : MonoBehaviour
{
    // --- FINITE STATE MACHINE ---
    public enum BossState
    {
        Intro, Chase, Melee, Throw, Slam, Scream, Dead
    }
    [Header("State Machine")]
    public BossState currentState = BossState.Intro;

    [Header("Boss Stats")]
    public int maxHealth = 500; 
    private int currentHealth; 

    [Header("Movement & Targeting")]
    public float turnSpeed = 5f; 
    private Transform player; 

    [Header("Attack Timings & Cooldowns")]
    public float stopChaseDistance = 7.0f;
    public float meleeRange = 4.0f;
    public float slamRange = 9.0f; 
    public float throwMinRange = 12.0f; 
    
    public float meleeDamage = 25f;
    public float attackCooldown = 2.0f;
    public float throwCooldown = 5.0f; 
    public float screamCooldown = 15.0f;
    public float slamCooldown = 10.0f;

    private float lastMeleeTime; 
    private float lastThrowTime; 
    private float lastScreamTime; 
    private float lastSlamTime; 

    [Header("Throw Attack Projectile")]
    public GameObject projectilePrefab; 
    public Transform handSpawnPoint; 
    public float throwForce = 25f; // Added adjustable force
    [Tooltip("Time in seconds to wait before the rock actually spawns/fires")]
    public float throwReleaseDelay = 0.85f; 
    [Tooltip("Height offset to aim higher than the player's root (e.g., 1.5 = chest/head height)")]
    public float throwAimHeightOffset = 1.6f;

    [Header("Hit Visuals & FX")]
    public GameObject[] bloodEffectPrefabs; 
    public float corpseDespawnDelay = 5.0f; // Time the boss body stays on the ground
    public SkinnedMeshRenderer targetRenderer;
    public Material normalMaterial; 
    public Material hitFlashMaterial; 
    public float flashDuration = 0.08f; 
    private Coroutine flashRoutine; 

    [Header("Power-Up Visuals & Weak Zones")]
    public GameObject gogglesIndicator; 
    public Collider[] possibleWeakZones; 
    public GameObject[] weakZoneVisuals; 
    private Collider activeWeakZoneCollider; 

    // Trackers for GameManager states
    private bool wasGogglesActive;
    private bool wasWeakZoneActive;

    private Animator animator; 
    private List<BulletMark> attachedBulletMarks = new List<BulletMark>();

    [Header("Cutscene Animation Settings")]
    public float jumpAnimDuration = 1.2f; 

    [Header("Audio & SFX")]
    public AudioSource audioSource;
    public AudioClip roarSFX;
    
    void Awake()
    {
        animator = GetComponent<Animator>(); 

        if (targetRenderer != null && normalMaterial == null) 
        {
            normalMaterial = targetRenderer.sharedMaterial; 
        }
        FindPlayer(); 
    }

    void OnEnable()
    {
        currentHealth = maxHealth; 
        
        lastMeleeTime = -attackCooldown; 
        lastThrowTime = Time.time; 
        lastScreamTime = Time.time + 5f; 
        lastSlamTime = Time.time + 8f; 

        currentState = BossState.Chase;

        ResetToNormalMaterial(); 
        FindPlayer(); 

        activeWeakZoneCollider = null; 
        
        // Sync initial power-up states
        if (GameManager.Instance != null)
        {
            wasGogglesActive = GameManager.Instance.isGogglesActive;
            wasWeakZoneActive = GameManager.Instance.isWeakZoneActive;
        }
        RefreshPowerUpVisuals(); 
    }

    public void PlayRoarSFX()
    {
        if (audioSource != null && roarSFX != null)
        {
            audioSource.PlayOneShot(roarSFX);
        }
    }

    public void PlayIntroAnimation()
    {
        StartCoroutine(IntroCutsceneSequence());
    }

    private IEnumerator IntroCutsceneSequence()
    {
        if (animator != null) animator.SetTrigger("Jump");
        yield return new WaitForSeconds(jumpAnimDuration);
        if (animator != null) animator.SetTrigger("Scream");
    }

    void OnDisable()
    {
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

    public void ForceFacePlayer()
    {
        if (player == null) FindPlayer(); 
        if (player != null) 
        {
            Vector3 lookDir = (player.position - transform.position).normalized; 
            lookDir.y = 0; 
            if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir); 
        }
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
        if (player == null || currentHealth <= 0 || currentState == BossState.Dead) return;

        // Auto-detect if player picked up a power-up without relying on GameManager's enemy list
        if (GameManager.Instance != null)
        {
            if (wasGogglesActive != GameManager.Instance.isGogglesActive || 
                wasWeakZoneActive != GameManager.Instance.isWeakZoneActive)
            {
                wasGogglesActive = GameManager.Instance.isGogglesActive;
                wasWeakZoneActive = GameManager.Instance.isWeakZoneActive;
                RefreshPowerUpVisuals();
            }
        }

        switch (currentState)
        {
            case BossState.Chase:
                HandleChaseState();
                break;
            case BossState.Melee:
            case BossState.Throw:
            case BossState.Slam:
            case BossState.Scream:
            case BossState.Intro:
                break;
        }
    }

    void HandleChaseState()
    {
        Vector3 lookDir = (player.position - transform.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > stopChaseDistance)
        {
            if (animator != null) animator.SetFloat("Speed", 1f); 
        }
        else
        {
            if (animator != null) animator.SetFloat("Speed", 0f); 
        }

        if (distanceToPlayer <= meleeRange && Time.time >= lastMeleeTime + attackCooldown)
        {
            StartCoroutine(ExecuteMeleeAttack());
        }
        else if (distanceToPlayer >= throwMinRange && Time.time >= lastThrowTime + throwCooldown)
        {
            StartCoroutine(ExecuteThrowAttack());
        }
        else if (distanceToPlayer <= slamRange && Time.time >= lastSlamTime + slamCooldown)
        {
            StartCoroutine(ExecuteJumpSlam());
        }
        else if (Time.time >= lastScreamTime + screamCooldown)
        {
            StartCoroutine(ExecuteScream());
        }
    }

    IEnumerator ExecuteMeleeAttack()
    {
        currentState = BossState.Melee;
        lastMeleeTime = Time.time; 
        if (animator != null) { animator.SetFloat("Speed", 0f); animator.SetTrigger("Attack"); } 

        yield return new WaitForSeconds(0.5f); 

        if (currentHealth > 0 && player != null) 
        {
            if (Vector3.Distance(transform.position, player.position) <= meleeRange + 1f) 
            {
                if (GameManager.Instance != null) GameManager.Instance.TakePlayerDamage(meleeDamage); 
            }
        }

        yield return new WaitForSeconds(0.8f); 
        currentState = BossState.Chase; 
    }

    IEnumerator ExecuteThrowAttack()
    {
        currentState = BossState.Throw;
        lastThrowTime = Time.time;
        if (animator != null) { animator.SetFloat("Speed", 0f); animator.SetTrigger("Throw"); }

        // Wait for the exact moment the hand releases the rock in the animation
        yield return new WaitForSeconds(throwReleaseDelay);

        if (projectilePrefab != null && player != null)
        {
            Vector3 spawnPos = handSpawnPoint != null ? handSpawnPoint.position : transform.position + Vector3.up * 2f;
            GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            // Point projectile at the player with the new higher offset
            Vector3 targetCenter = player.position + (Vector3.up * throwAimHeightOffset);
            Vector3 throwDir = (targetCenter - spawnPos).normalized;
            proj.transform.forward = throwDir;

            Rigidbody rb = proj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = throwDir * throwForce;
            }
        }

        // Wait a brief moment for follow-through before resuming chase
        yield return new WaitForSeconds(0.8f);
        currentState = BossState.Chase;
    }

    IEnumerator ExecuteJumpSlam()
    {
        currentState = BossState.Slam;
        lastSlamTime = Time.time; 
        if (animator != null) { animator.SetFloat("Speed", 0f); animator.SetTrigger("Jump"); } 

        yield return new WaitForSeconds(0.8f); 

        if (player != null && Vector3.Distance(transform.position, player.position) <= meleeRange * 2f) 
        {
            if (GameManager.Instance != null) GameManager.Instance.TakePlayerDamage(meleeDamage * 1.5f); 
        }

        yield return new WaitForSeconds(0.8f); 
        currentState = BossState.Chase;
    }

    IEnumerator ExecuteScream()
    {
        currentState = BossState.Scream;
        lastScreamTime = Time.time; 
        if (animator != null) { animator.SetFloat("Speed", 0f); animator.SetTrigger("Scream"); } 

        yield return new WaitForSeconds(1.5f); 
        currentState = BossState.Chase;
    }

    // --- BLOOD FX INSTANTIATION ---
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
        if (currentState == BossState.Dead) return;

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
        if (animator != null && currentState != BossState.Dead) animator.SetTrigger("Hit"); 

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

    IEnumerator DieSequence()
    {
        currentState = BossState.Dead;

        // 1. Play death animation
        if (animator != null)
        {
            animator.SetTrigger("Dead");
        }

        // 2. Disable main collider so the player/projectiles don't hit the corpse
        Collider bossCollider = GetComponent<Collider>();
        if (bossCollider != null) bossCollider.enabled = false;

        // 3. Clear attached bullet marks
        DeactivateAllBulletMarks(); 

        // 4. Update Score
        if (GameManager.Instance != null) 
        {
            GameManager.Instance.AddScore(100); 
        }

        // 5. Keep corpse in scene temporarily
        yield return new WaitForSeconds(corpseDespawnDelay);

        gameObject.SetActive(false); 
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

    public void RefreshPowerUpVisuals()
    {
        if (GameManager.Instance == null) return; 

        if (gogglesIndicator != null) gogglesIndicator.SetActive(GameManager.Instance.isGogglesActive); 

        if (GameManager.Instance.isWeakZoneActive) 
        {
            if (activeWeakZoneCollider == null && possibleWeakZones != null && possibleWeakZones.Length > 0) 
            {
                activeWeakZoneCollider = possibleWeakZones[Random.Range(0, possibleWeakZones.Length)]; 
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
        if (GameManager.Instance == null || !GameManager.Instance.isWeakZoneActive || activeWeakZoneCollider == null) return false; 
        return hitCollider == activeWeakZoneCollider || hitCollider.transform.IsChildOf(activeWeakZoneCollider.transform) || activeWeakZoneCollider.transform.IsChildOf(hitCollider.transform); 
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

    void Die()
    {
        currentState = BossState.Dead;
        DeactivateAllBulletMarks(); 

        if (GameManager.Instance != null) GameManager.Instance.AddScore(100); 
        gameObject.SetActive(false); 
    }

    void OnDestroy()
    {
        DeactivateAllBulletMarks(); 
    }
}