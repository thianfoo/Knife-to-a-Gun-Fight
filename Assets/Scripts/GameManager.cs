using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float regenDelay = 5f;
    public float regenRate = 20f;
    private float lastHitTime;
    private bool isPlayerDead = false;

    [Header("Delayed Health Bar Settings")]
    public Slider healthSlider;           // Front Bar (instant)
    public Slider delayedHealthSlider;    // Back Bar (ghost damage)
    public float damageDrainDelay = 0.5f;
    public float damageDrainSpeed = 35f;
    private float lastDamageTime;

    [Header("UI Overlays")]
    public CanvasGroup bloodyScreen;
    public GameObject deathScreenPanel;   // Drag DeathScreenPanel here
    public TextMeshProUGUI scoreEndText;

    [Header("Score & Spawning")]
    public TextMeshProUGUI scoreText;
    public int score = 0;
    public GameObject enemyPrefab;
    public int poolSize = 30;
    public Transform[] spawnPoints;
    private List<GameObject> enemyPool = new List<GameObject>();

    [Header("Wave Stats")]
    public int currentWave = 1;
    public int enemiesToSpawn = 5;
    private int activeEnemies = 0;
    private Coroutine spawnWaveCoroutine;

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

        // Reset Sliders
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

        // Reset overlays
        if (bloodyScreen != null) bloodyScreen.alpha = 0f;
        if (deathScreenPanel != null) deathScreenPanel.SetActive(false);

        // Lock cursor back into FPS mode
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        UpdateScoreDisplay();

        // Start initial wave
        if (spawnWaveCoroutine != null) StopCoroutine(spawnWaveCoroutine);
        spawnWaveCoroutine = StartCoroutine(SpawnWave());
    }

    void Update()
    {
        if (isPlayerDead) return;

        // 1. Health Regeneration
        if (currentHealth < maxHealth && Time.time > lastHitTime + regenDelay)
        {
            currentHealth += regenRate * Time.deltaTime;
            currentHealth = Mathf.Min(currentHealth, maxHealth);

            if (healthSlider != null) healthSlider.value = currentHealth;
            if (delayedHealthSlider != null) delayedHealthSlider.value = currentHealth;
        }

        // 2. Delayed Ghost Bar Drain
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

        // 3. Smooth Blood Overlay Fade
        if (bloodyScreen != null)
        {
            float targetAlpha = 1f - (currentHealth / maxHealth);
            bloodyScreen.alpha = Mathf.Lerp(bloodyScreen.alpha, targetAlpha, Time.deltaTime * 3f);
        }
    }

    public void TakePlayerDamage(float amount)
    {
        if (isPlayerDead) return;

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

        // Stop incoming wave spawns
        if (spawnWaveCoroutine != null)
        {
            StopCoroutine(spawnWaveCoroutine);
            spawnWaveCoroutine = null;
        }

        // Show death UI
        if (deathScreenPanel != null)
        {
            deathScreenPanel.SetActive(true);
        }

        // Unlock mouse cursor so the player can click Restart
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// 
    /// Call this method from the Restart Button's OnClick event
    /// 
    public void RestartGame()
    {
        // 1. Despawn all active enemies in pool
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

        // 2. Reset stats and start clean
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
        activeEnemies = enemiesToSpawn;
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            if (isPlayerDead) yield break;

            GameObject enemy = GetPooledEnemy();
            if (enemy != null)
            {
                Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

                UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null) agent.enabled = false;

                enemy.transform.position = spawnPoint.position;
                enemy.transform.rotation = spawnPoint.rotation;

                enemy.SetActive(true);

                if (agent != null)
                {
                    agent.enabled = true;
                    agent.Warp(spawnPoint.position);
                }
            }
            yield return new WaitForSeconds(1.5f);
        }
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
}