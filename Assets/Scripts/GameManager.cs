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

    [Header("Delayed Health Bar Settings")]
    public Slider healthSlider;           // Front Bar (instant)
    public Slider delayedHealthSlider;    // Back Bar (delayed "ghost" damage)
    public float damageDrainDelay = 0.5f; // Pause before the ghost bar begins draining
    public float damageDrainSpeed = 35f;  // Speed at which the ghost bar catches up
    private float lastDamageTime;

    [Header("Health UI")]
    public CanvasGroup bloodyScreen;

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

    void Awake()
    {
        Instance = this;
        InitializePool();
    }

    void Start()
    {
        currentHealth = maxHealth;

        // Initialize sliders
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

        if (bloodyScreen != null)
        {
            bloodyScreen.alpha = 0f;
        }

        UpdateScoreDisplay();
        StartCoroutine(SpawnWave());
    }

    void Update()
    {
        // 1. Health Regeneration
        if (currentHealth < maxHealth && Time.time > lastHitTime + regenDelay)
        {
            currentHealth += regenRate * Time.deltaTime;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            
            if (healthSlider != null) healthSlider.value = currentHealth;
            // On regen, make both bars rise together
            if (delayedHealthSlider != null) delayedHealthSlider.value = currentHealth;
        }

        // 2. Delayed "Ghost" Bar Drain (Catching down to current health)
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
        currentHealth -= amount;
        lastHitTime = Time.time;
        lastDamageTime = Time.time; // Starts the timer for the ghost bar delay

        if (currentHealth < 0) currentHealth = 0;

        // Instantly drop the primary health bar
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        // Flash blood overlay
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

    void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }

    void PlayerDied()
    {
        Debug.Log("Game Over! Player Health reached 0.");
        // Optional: show a game over canvas or restart the scene
    }

    #region Pooling & Wave Logic

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
            scoreText.text = $"SCORE: {score:D6}";
        }
    }

    void NextWave()
    {
        currentWave++;
        enemiesToSpawn += 3;
        StartCoroutine(SpawnWave());
    }

    #endregion
}