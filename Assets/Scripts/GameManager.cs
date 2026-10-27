using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Observer Pattern")]
    public ScoreEventChannel scoreChannel;

    [Header("Player Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float regenDelay = 5f;
    public float regenRate = 20f;
    private float lastHitTime;
    private bool isPlayerDead = false;

    [Header("Delayed Health Bar Settings")]
    public Slider healthSlider;           
    public Slider delayedHealthSlider;    
    public float damageDrainDelay = 0.5f;
    public float damageDrainSpeed = 35f;
    private float lastDamageTime;

    [Header("UI Overlays")]
    public CanvasGroup bloodyScreen;
    public GameObject deathScreenPanel;   
    public TextMeshProUGUI scoreEndText;

    [Header("Score & Spawning")]
    public TextMeshProUGUI scoreText;
    public int score = 0;
    public GameObject enemyPrefab;
    public int poolSize = 30;
    public Transform[] spawnPoints;
    private List<GameObject> enemyPool = new List<GameObject>();

    [Header("Spawn Obstruction Checks")]
    public float spawnClearanceRadius = 1.2f;
    public LayerMask spawnBlockerMask;

    [Header("Wave Stats")]
    public int currentWave = 1;
    public int enemiesToSpawn = 10;
    private int activeEnemies = 0;
    private Coroutine spawnWaveCoroutine;

    [Header("Player Respawn Settings")]
    public GameObject playerObject; 
    public Transform playerSpawn;

    [Header("Power-Up States (SO Architecture)")]
    public BoolVariable gogglesState;
    public BoolVariable weakZoneState;

    private Coroutine gogglesCoroutine;
    private Coroutine weakZoneCoroutine;

    [Header("Boss Cutscene Settings")]
    public int bossWave = 5;                        
    public Camera cutsceneCamera;                  
    public Transform bossCutsceneLookAtPoint;       
    public Transform bossCutsceneStartPoint;       
    public float cutsceneDuration = 4f;            
    public GameObject bossPrefab;                  
    public Transform bossSpawn;                        

    [Header("Boss Cutscene Camera Tuning")]
    public float cutsceneLookHeight = 0.8f; 
    public float shakeIntensity = 0.3f;
    public float shakeStartTime = 1.0f;

    private bool isCutscenePlaying = false;

    void Awake()
    {
        Instance = this;
        InitializePool();
    }

    void Start()
    {
        InitializeGame();
    }

    void InitializeGame()
    {
        isPlayerDead = false;
        currentHealth = maxHealth;
        score = 0;
        currentWave = 1;
        enemiesToSpawn = 5;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (delayedHealthSlider != null)
        {
            delayedHealthSlider.maxValue = maxHealth;
            delayedHealthSlider.value = currentHealth;
        }

        if (bloodyScreen != null) bloodyScreen.alpha = 0f;
        if (deathScreenPanel != null) deathScreenPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        UpdateScoreDisplay();

        if (spawnWaveCoroutine != null) StopCoroutine(spawnWaveCoroutine);
        spawnWaveCoroutine = StartCoroutine(SpawnWave());
    }

    void Update()
    {
        if (playerObject == null)
        {
            int targetLayer = LayerMask.NameToLayer("PlayerRoot");
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (GameObject obj in allObjects)
            {
                if (obj.layer == targetLayer)
                {
                    playerObject = obj.transform.root.gameObject;
                    break;
                }
            }
        }

        if (isPlayerDead) return;

        if (currentHealth < maxHealth && Time.time > lastHitTime + regenDelay)
        {
            currentHealth += regenRate * Time.deltaTime;
            currentHealth = Mathf.Min(currentHealth, maxHealth);

            if (healthSlider != null) healthSlider.value = currentHealth;
            if (delayedHealthSlider != null) delayedHealthSlider.value = currentHealth;
        }

        if (delayedHealthSlider != null && delayedHealthSlider.value > currentHealth)
        {
            if (Time.time >= lastDamageTime + damageDrainDelay)
            {
                delayedHealthSlider.value = Mathf.MoveTowards(
                    delayedHealthSlider.value,
                    currentHealth,
                    damageDrainSpeed * Time.deltaTime
                );
            }
        }

        if (bloodyScreen != null)
        {
            float targetAlpha = 1f - (currentHealth / maxHealth);
            bloodyScreen.alpha = Mathf.Lerp(bloodyScreen.alpha, targetAlpha, Time.deltaTime * 3f);
        }
    }

    public void TakePlayerDamage(float amount)
    {
        if (isPlayerDead || isCutscenePlaying) return;

        currentHealth -= amount;
        lastHitTime = Time.time;
        lastDamageTime = Time.time;

        if (currentHealth < 0) currentHealth = 0;

        if (healthSlider != null) healthSlider.value = currentHealth;

        if (bloodyScreen != null)
        {
            float targetAlpha = 1f - (currentHealth / maxHealth);
            bloodyScreen.alpha = Mathf.Clamp01(targetAlpha + 0.3f);
        }

        if (currentHealth <= 0)
        {
            PlayerDied();
        }
    }

    void PlayerDied()
    {
        isPlayerDead = true;

        if (spawnWaveCoroutine != null)
        {
            StopCoroutine(spawnWaveCoroutine);
            spawnWaveCoroutine = null;
        }

        if (deathScreenPanel != null)
        {
            deathScreenPanel.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        ResetPowerUps();

        for (int i = 0; i < enemyPool.Count; i++)
        {
            if (enemyPool[i] != null && enemyPool[i].activeInHierarchy)
            {
                EnemyAI enemyAI = enemyPool[i].GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    enemyAI.DeactivateAllBulletMarks();
                }
                enemyPool[i].SetActive(false);
            }
        }

        BossAI[] activeBosses = FindObjectsByType<BossAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (BossAI boss in activeBosses)
        {
            Destroy(boss.gameObject);
        }

        BossProjectile[] projectiles = FindObjectsByType<BossProjectile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (BossProjectile proj in projectiles)
        {
            Destroy(proj.gameObject);
        }

        if (playerObject != null && playerSpawn != null)
        {
            CharacterController cc = playerObject.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerObject.transform.position = playerSpawn.position;
            playerObject.transform.rotation = playerSpawn.rotation;

            if (cc != null) cc.enabled = true;
        }
        else
        {
            Debug.LogWarning("Player Object or Player Spawn is not assigned in the GameManager!");
        }

        InitializeGame();
    }

    void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject enemy = Instantiate(enemyPrefab);
            enemy.SetActive(false);
            enemyPool.Add(enemy);
        }
    }

    public GameObject GetPooledEnemy()
    {
        foreach (GameObject enemy in enemyPool)
        {
            if (!enemy.activeInHierarchy)
            {
                return enemy;
            }
        }
        return null;
    }

    IEnumerator SpawnWave()
    {
        activeEnemies = 0;
        int spawnedCount = 0;
        int maxAttemptsPerEnemy = 10;

        if (currentWave == bossWave && bossPrefab != null)
        {
            Transform chosenBossSpawn = bossSpawn != null ? bossSpawn : spawnPoints[0];
            GameObject bossObj = Instantiate(bossPrefab, chosenBossSpawn.position, chosenBossSpawn.rotation);
            activeEnemies = 1;

            BossAI bossAI = bossObj.GetComponent<BossAI>();
            if (bossAI != null)
            {
                bossAI.enabled = false; 
                bossAI.PlayIntroAnimation(); 
            }

            bossCutsceneLookAtPoint = bossObj.transform;

            yield return StartCoroutine(PlayBossCutscene(bossObj));

            if (bossAI != null)
            {
                bossAI.enabled = true;
                bossAI.ForceFacePlayer(); 
            }

            yield break;
        }

        while (spawnedCount < enemiesToSpawn)
        {
            if (isPlayerDead) yield break;

            GameObject enemy = GetPooledEnemy();
            if (enemy == null)
            {
                yield return new WaitForSeconds(1.0f);
                continue;
            }

            Transform chosenPoint = null;
            for (int attempt = 0; attempt < maxAttemptsPerEnemy; attempt++)
            {
                Transform candidate = spawnPoints[Random.Range(0, spawnPoints.Length)];
                bool isBlocked = Physics.CheckSphere(candidate.position, spawnClearanceRadius, spawnBlockerMask);
                if (!isBlocked)
                {
                    chosenPoint = candidate;
                    break;
                }
            }

            if (chosenPoint == null)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            enemy.transform.position = chosenPoint.position;
            enemy.transform.rotation = chosenPoint.rotation;
            enemy.SetActive(true);

            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.RefreshPowerUpVisuals();
            }

            if (agent != null)
            {
                agent.enabled = true;
                agent.Warp(chosenPoint.position);
            }

            spawnedCount++;
            activeEnemies++;

            yield return new WaitForSeconds(1.5f);
        }
    }

    IEnumerator PlayBossCutscene(GameObject boss)
    {
        isCutscenePlaying = true;

        Camera playerCam = null;
        if (playerObject != null)
        {
            playerCam = playerObject.GetComponentInChildren<Camera>();
        }
        if (playerCam == null) playerCam = Camera.main;

        if (playerCam != null) playerCam.enabled = false;
        if (cutsceneCamera != null)
        {
            cutsceneCamera.enabled = true;
            if (bossCutsceneStartPoint != null)
            {
                cutsceneCamera.transform.position = bossCutsceneStartPoint.position;
                cutsceneCamera.transform.rotation = bossCutsceneStartPoint.rotation;
            }
        }

        float elapsed = 0f;
        Vector3 baseCameraPos = cutsceneCamera != null ? cutsceneCamera.transform.position : Vector3.zero;
        Quaternion startRot = cutsceneCamera != null ? cutsceneCamera.transform.rotation : Quaternion.identity;

        while (elapsed < cutsceneDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / cutsceneDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (cutsceneCamera != null && boss != null)
            {
                Vector3 targetFocusPos = boss.transform.position + Vector3.up * cutsceneLookHeight;
                Vector3 lookDir = (targetFocusPos - baseCameraPos).normalized;

                if (lookDir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir);
                    cutsceneCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
                }

                if (elapsed >= shakeStartTime)
                {
                    float shakeDamp = 1f - ((elapsed - shakeStartTime) / (cutsceneDuration - shakeStartTime));
                    float currentIntensity = shakeIntensity * Mathf.Max(0f, shakeDamp);

                    Vector3 shakeOffset = new Vector3(
                        Random.Range(-1f, 1f) * currentIntensity,
                        Random.Range(-1f, 1f) * currentIntensity,
                        Random.Range(-1f, 1f) * currentIntensity
                    );

                    cutsceneCamera.transform.position = baseCameraPos + shakeOffset;
                }
                else
                {
                    cutsceneCamera.transform.position = baseCameraPos;
                }
            }

            yield return null;
        }

        if (cutsceneCamera != null)
        {
            cutsceneCamera.transform.position = baseCameraPos;
            cutsceneCamera.enabled = false;
        }
        
        if (playerCam != null) playerCam.enabled = true;

        isCutscenePlaying = false;
    }

    public void AddScore(int points)
    {
        if (isPlayerDead) return;

        score += points;
        UpdateScoreDisplay();

        activeEnemies--;
        if (activeEnemies <= 0)
        {
            NextWave();
        }
    }

    void OnEnable()
    {
        if (scoreChannel != null)
        {
            scoreChannel.OnScoreAdded += AddScore;
        }
    }

    void OnDisable()
    {
        if (scoreChannel != null)
        {
            scoreChannel.OnScoreAdded -= AddScore;
        }
    }

    void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"SCORE: {score:D5}";
        }
        if (scoreEndText != null)
        {
            scoreEndText.text = $"FINAL SCORE: {score:D5}";
        }
    }

    void NextWave()
    {
        currentWave++;
        enemiesToSpawn += 3;
        spawnWaveCoroutine = StartCoroutine(SpawnWave());
    }

    public void ActivateGoggles(float duration)
    {
        if (gogglesCoroutine != null) StopCoroutine(gogglesCoroutine);
        gogglesCoroutine = StartCoroutine(GogglesRoutine(duration));
    }

    private IEnumerator GogglesRoutine(float duration)
    {
        if (gogglesState != null) gogglesState.Value = true;

        yield return new WaitForSeconds(duration);

        if (gogglesState != null) gogglesState.Value = false;
        gogglesCoroutine = null;
    }

    public void ActivateWeakZones(float duration)
    {
        if (weakZoneCoroutine != null) StopCoroutine(weakZoneCoroutine);
        weakZoneCoroutine = StartCoroutine(WeakZoneRoutine(duration));
    }

    private IEnumerator WeakZoneRoutine(float duration)
    {
        if (weakZoneState != null) weakZoneState.Value = true;

        yield return new WaitForSeconds(duration);

        if (weakZoneState != null) weakZoneState.Value = false;
        weakZoneCoroutine = null;
    }

    void ResetPowerUps()
    {
        if (gogglesCoroutine != null)
        {
            StopCoroutine(gogglesCoroutine);
            gogglesCoroutine = null;
        }

        if (weakZoneCoroutine != null)
        {
            StopCoroutine(weakZoneCoroutine);
            weakZoneCoroutine = null;
        }

        if (gogglesState != null) gogglesState.Value = false;
        if (weakZoneState != null) weakZoneState.Value = false;
    }
}