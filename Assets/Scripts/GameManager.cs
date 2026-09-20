using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // If using UI text for score

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Spawning & Pooling")]
    public GameObject enemyPrefab;
    public int poolSize = 30;
    public Transform[] spawnPoints;
    private List<GameObject> enemyPool = new List<GameObject>();

    [Header("Wave Stats")]
    public int currentWave = 1;
    public int enemiesToSpawn = 5;
    private int activeEnemies = 0;

    [Header("Scoring")]
    public int score = 0;

    void Awake()
    {
        Instance = this;
        InitializePool();
    }

    void Start()
    {
        StartCoroutine(SpawnWave());
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

    // Finds a hidden enemy in the pool and activates it
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
                // Pick a random spawn point
                Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                enemy.transform.position = spawnPoint.position;
                enemy.transform.rotation = spawnPoint.rotation;
                enemy.SetActive(true);
            }
            yield return new WaitForSeconds(1.5f); // Delay between spawns
        }
    }

    public void AddScore(int points)
    {
        score += points;
        Debug.Log("Score: " + score);
        
        activeEnemies--;
        if (activeEnemies <= 0)
        {
            NextWave();
        }
    }

    void NextWave()
    {
        currentWave++;
        enemiesToSpawn += 3; // Increase difficulty
        Debug.Log("Wave " + currentWave + " starting!");
        StartCoroutine(SpawnWave());
    }
}