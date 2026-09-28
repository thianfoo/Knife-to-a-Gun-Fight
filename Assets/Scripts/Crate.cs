using System.Collections;
using UnityEngine;
using TMPro;

public class Crate : MonoBehaviour
{
    [Header("Detection Settings")]
    public float interactionDistance = 3.5f;
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("The layer name your player is on, matching your EnemyAI script")]
    public string playerLayerName = "PlayerRoot";

    [Header("UI Prompt")]
    public GameObject promptCanvas;

    [Header("Anticipation Animation (In Range)")]
    public float bounceHeight = 0.3f;
    public float bounceSpeed = 6f;
    public float shakeAngle = 8f;
    public float shakeSpeed = 15f;

    [Header("Open Animation (Fly & Shrink)")]
    public float openDuration = 1.2f;
    public float flyUpwardDistance = 2.5f;
    public float spinSpeed = 1000f; // Degrees per second

    // Internal State
    private Transform player;
    private Camera mainCamera;
    
    private bool isPlayerNearby = false;
    private bool isOpened = false;

    // Caching original transforms
    private Vector3 basePosition;
    private Quaternion baseRotation;
    private Vector3 baseScale;

    void Awake()
    {
        mainCamera = Camera.main;
        
        // Cache the starting position, rotation, and scale so we can return to them
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;
        baseScale = transform.localScale;
    }

    void Start()
    {
        FindPlayer();

        if (promptCanvas != null)
        {
            promptCanvas.SetActive(false);
        }
    }

    // Identical player detection to your EnemyAI script
    void FindPlayer()
    {
        int playerLayer = LayerMask.NameToLayer(playerLayerName);

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
        if (isOpened || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= interactionDistance)
        {
            // Player is in range
            if (!isPlayerNearby)
            {
                isPlayerNearby = true;
                if (promptCanvas != null) promptCanvas.SetActive(true);
            }

            // Billboard the UI prompt
            if (promptCanvas != null && promptCanvas.activeSelf && mainCamera != null)
            {
                promptCanvas.transform.rotation = Quaternion.LookRotation(
                    promptCanvas.transform.position - mainCamera.transform.position
                );
            }

            // 1. Procedural Bounce & Shake (Anticipation)
            // Using Mathf.Abs(Sin) creates a bouncing effect off the ground
            float bounceOffset = Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed)) * bounceHeight;
            float wiggleOffset = Mathf.Sin(Time.time * shakeSpeed) * shakeAngle;

            transform.localPosition = basePosition + new Vector3(0f, bounceOffset, 0f);
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, wiggleOffset);

            // 2. Check for Input
            if (Input.GetKeyDown(interactKey))
            {
                OpenChest();
            }
        }
        else
        {
            // Player is out of range
            if (isPlayerNearby)
            {
                isPlayerNearby = false;
                if (promptCanvas != null) promptCanvas.SetActive(false);
            }

            // Smoothly settle the chest back to its resting position if it was bouncing
            if (transform.localPosition != basePosition || transform.localRotation != baseRotation)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, basePosition, Time.deltaTime * 5f);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, baseRotation, Time.deltaTime * 5f);
            }
        }
    }

    void OpenChest()
    {
        isOpened = true;

        if (promptCanvas != null)
        {
            promptCanvas.SetActive(false);
        }

        // Add loot/score logic here
        // GameManager.Instance.AddScore(50);

        StartCoroutine(FlyAndShrinkRoutine());
    }

    IEnumerator FlyAndShrinkRoutine()
    {
        float elapsed = 0f;
        Vector3 startPos = transform.localPosition;
        Vector3 targetPos = startPos + new Vector3(0f, flyUpwardDistance, 0f);

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / openDuration;

            // 1. Spin wildly on the Y axis
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

            // 2. Fly upwards (using ease-out so it slows down at the top)
            float easeOut = Mathf.Sin(normalizedTime * Mathf.PI * 0.5f);
            transform.localPosition = Vector3.Lerp(startPos, targetPos, easeOut);

            // 3. Shrink down to zero
            transform.localScale = Vector3.Lerp(baseScale, Vector3.zero, normalizedTime);

            yield return null;
        }

        // Ensure it's fully hidden at the end
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }
}