using UnityEngine;

public class Tracker : MonoBehaviour
{
    [Header("Targeting Settings")]
    public string enemyTag = "Enemy";
    public string bossTag = "Boss";
    public float rotationSpeed = 12f;

    [Header("Goggles Integration (SO Architecture)")]
    [Tooltip("Check this if you only want the pointer visible when Goggles power-up is active")]
    public bool requireGogglesPowerUp = false;
    public BoolVariable gogglesState;

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

    void OnEnable()
    {
        // Subscribe to Goggles State event
        if (gogglesState != null)
        {
            gogglesState.OnValueChanged += OnGogglesStateChanged;
        }
    }

    void OnDisable()
    {
        // Unsubscribe from Goggles State event
        if (gogglesState != null)
        {
            gogglesState.OnValueChanged -= OnGogglesStateChanged;
        }

        if (arrow != null) arrow.SetActive(false);
    }

    private void OnGogglesStateChanged(bool isActive)
    {
        // Instantly hide arrow if power-up turns off
        if (requireGogglesPowerUp && !isActive && arrow != null)
        {
            arrow.SetActive(false);
        }
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

        // 2. Check Goggles state via Scriptable Object
        if (requireGogglesPowerUp)
        {
            bool isGogglesActive = gogglesState != null && gogglesState.Value;
            if (!isGogglesActive)
            {
                if (arrow != null) arrow.SetActive(false); 
                return;
            }
        }

        // 3. Find closest active enemy or boss
        FindNearestTarget(); 

        // Hide pointer if no enemies/boss exist
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
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == playerLayerIndex)
            {
                playerTransform = obj.transform.root;

                // Parent directly to player root so it moves with the camera
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

        // Smoothly lerp position if unparented
        if (transform.parent != playerTransform)
        {
            Vector3 targetPosition = playerTransform.TransformPoint(orbitOffset);
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
        }
    }

    void FindNearestTarget()
    {
        float closestDist = Mathf.Infinity;
        Transform closest = null;

        // Check normal enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        foreach (GameObject enemy in enemies)
        {
            if (enemy == null || !enemy.activeInHierarchy) continue;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy.transform;
            }
        }

        // Check boss objects
        GameObject[] bosses = GameObject.FindGameObjectsWithTag(bossTag);
        foreach (GameObject boss in bosses)
        {
            if (boss == null || !boss.activeInHierarchy) continue;

            float dist = Vector3.Distance(transform.position, boss.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = boss.transform;
            }
        }

        targetEnemy = closest;
    }

    void RotateTowardsEnemy()
    {
        if (targetEnemy == null) return;

        Vector3 directionToEnemy = (targetEnemy.position - transform.position).normalized;

        if (directionToEnemy != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToEnemy);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
}