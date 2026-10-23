using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossAI : MonoBehaviour
{
    // --- FINITE STATE MACHINE ---
    public enum BossState
    {
        Intro,
        Chase,
        Melee,
        Throw,
        Slam,
        Scream,
        Dead
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
    public float meleeRange = 4.0f; 
    public float throwMinRange = 8.0f; // Throws only if player is further than this
    public float meleeDamage = 25f; 
    
    public float attackCooldown = 2.0f; 
    public float throwCooldown = 6.0f; 
    public float screamCooldown = 15.0f; 
    public float slamCooldown = 10.0f; 

    private float lastMeleeTime; 
    private float lastThrowTime; 
    private float lastScreamTime; 
    private float lastSlamTime; 

    [Header("Throw Attack Projectile")]
    public GameObject projectilePrefab; 
    public Transform handSpawnPoint; 

    [Header("Hit Flash Setup")]
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

    private Animator animator; 
    private List<BulletMark> attachedBulletMarks = new List<BulletMark>(); 

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
        
        // Initialize cooldowns so he doesn't spam instantly
        lastMeleeTime = -attackCooldown; 
        lastThrowTime = Time.time; // Delay first throw 
        lastScreamTime = Time.time + 5f; // Delay first scream
        lastSlamTime = Time.time + 8f; 

        // GameManager enables this script after the cutscene ends, so go straight to Chase
        currentState = BossState.Chase;

        ResetToNormalMaterial(); 
        FindPlayer(); 

        activeWeakZoneCollider = null; 
        RefreshPowerUpVisuals(); 
    }

    public void PlayIntroAnimation()
    {
        if (animator != null) 
        {
            animator.SetTrigger("Jump"); 
        }
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
            if (lookDir != Vector3.zero) 
            {
                transform.rotation = Quaternion.LookRotation(lookDir); 
            }
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

        switch (currentState)
        {
            case BossState.Chase:
                HandleChaseState();
                break;
            
            // If in any attack state, we just wait for the Coroutine to finish and return us to Chase
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
        // 1. Rotate towards player
        Vector3 lookDir = (player.position - transform.position).normalized; 
        lookDir.y = 0; 
        if (lookDir != Vector3.zero) 
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir); 
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed); 
        }

        // 2. Play walk/run animation (Root Motion moves the transform)
        if (animator != null)
        {
            animator.SetFloat("Speed", 1f); // Ensures Animator transitions to movement state
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position); 

        // 3. FSM Transitions based on distance and cooldowns
        if (distanceToPlayer <= meleeRange && Time.time >= lastMeleeTime + attackCooldown) 
        {
            StartCoroutine(ExecuteMeleeAttack()); 
        }
        else if (distanceToPlayer >= throwMinRange && Time.time >= lastThrowTime + throwCooldown)
        {
            StartCoroutine(ExecuteThrowAttack()); 
        }
        else if (Time.time >= lastSlamTime + slamCooldown) 
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
        currentState = BossState.Chase; // Return to chase
    }

    IEnumerator ExecuteThrowAttack()
    {
        currentState = BossState.Throw;
        lastThrowTime = Time.time; 
        if (animator != null) { animator.SetFloat("Speed", 0f); animator.SetTrigger("Throw"); } 

        yield return new WaitForSeconds(0.6f); 

        if (projectilePrefab != null && player != null) 
        {
            Vector3 spawnPos = handSpawnPoint != null ? handSpawnPoint.position : transform.position + Vector3.up * 2f; 
            GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity); 

            Rigidbody rb = proj.GetComponent<Rigidbody>(); 
            if (rb != null) 
            {
                Vector3 throwDir = (player.position + Vector3.up - spawnPos).normalized; 
                rb.AddForce(throwDir * 22f, ForceMode.Impulse); 
            }
        }

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

    public void TakeDamage(int damage)
    {
        currentHealth -= damage; 

        if (flashRoutine != null) StopCoroutine(flashRoutine); 
        flashRoutine = StartCoroutine(DamageFlashRoutine()); 

        if (currentHealth <= 0) 
        {
            Die(); 
        }
    }

    IEnumerator DamageFlashRoutine()
    {
        if (animator != null) animator.SetTrigger("Hit"); 

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