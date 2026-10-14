using UnityEngine;

public class Tracker : MonoBehaviour
{
    [Header("Targeting Settings")]
    public string enemyTag = "Enemy";
    public float rotationSpeed = 12f;

    [Header("Goggles Integration")]
    [Tooltip("Check this if you only want the pointer visible when Goggles power-up is active")]
    public bool requireGogglesPowerUp = false;

    [Header("Player Tracking & Orbit")]
    public string playerLayerName = "PlayerRoot";
    [Tooltip("Offset relative to the player's root position (e.g., floating slightly above and in front)")]
    public Vector3 orbitOffset = new Vector3(0.4f, 1.2f, 0.8f);
    public float followSpeed = 10f;

    [Header("Visual Feedback")]
    public GameObject arrow;

    private Transform playerTransform;
    private Transform targetEnemy;
    private int playerLayerIndex;

    void Start()
    {
        playerLayerIndex = LayerMask.NameToLayer(playerLayerName);
    }

    void Update()
    {
        // 1. Locate Player if not already assigned
        if (playerTransform == null)
        {
            FindPlayerByLayer(); 

            if (playerTransform == null)
            {
                if (arrow != null) arrow.SetActive(false); 
                return; 
            }
        }

        // 2. Check Goggles state if required
        if (requireGogglesPowerUp && GameManager.Instance != null)
        {
            if (!GameManager.Instance.isGogglesActive)
            {
                if (arrow != null) arrow.SetActive(false); 
                return;
            }
        }

        // 3. Find closest active enemy
        FindNearestEnemy(); 

        // Hide pointer if no enemies exist
        if (targetEnemy == null) 
        {
            if (arrow != null) arrow.SetActive(false); 
            return; 
        }

        // Enable renderer ONLY if all checks passed
        if (arrow != null && !arrow.activeSelf) 
        {
            arrow.SetActive(true);
        }

        // 4. Update Position & Rotate
        FollowAndOrbitPlayer(); 
        RotateTowardsEnemy(); 
    }

    void FindPlayerByLayer()
    {
        // Search active scene objects for the PlayerRoot layer
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == playerLayerIndex)
            {
                // Grab the topmost root parent transform of the player
                playerTransform = obj.transform.root;

                // Option A: Parent directly so it moves seamlessly with player camera movement
                transform.SetParent(playerTransform);
                transform.localPosition = orbitOffset;
                transform.localRotation = Quaternion.identity;
                break;
            }
        }
    }

    void FollowAndOrbitPlayer()
    {
        if (playerTransform == null) return;

        // If not parented, smoothly lerp to the offset position relative to the player
        if (transform.parent != playerTransform)
        {
            Vector3 targetPosition = playerTransform.TransformPoint(orbitOffset);
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
        }
    }

    void FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);

        float closestDist = Mathf.Infinity;
        Transform closest = null;

        foreach (GameObject enemy in enemies)
        {
            // Ignore missing, destroyed, or inactive pooled enemies
            if (enemy == null || !enemy.activeInHierarchy) continue;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy.transform;
            }
        }

        targetEnemy = closest;
    }

    void RotateTowardsEnemy()
    {
        if (targetEnemy == null) return;

        // Calculate direction vector from tracker to enemy
        Vector3 directionToEnemy = (targetEnemy.position - transform.position).normalized;

        if (directionToEnemy != Vector3.zero)
        {
            // Calculate 3D rotation looking towards target
            Quaternion targetRotation = Quaternion.LookRotation(directionToEnemy);

            // Smoothly rotate toward enemy
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
}