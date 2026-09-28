using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; // 1. Added the New Input System namespace

[RequireComponent(typeof(BoxCollider))]
public class Crate : MonoBehaviour
{
    [Header("Detection Settings")]
    // 2. Changed KeyCode to Key for the new system
    public Key interactKey = Key.I; 
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
    public float spinSpeed = 1000f;

    // Internal State
    private Camera mainCamera;
    private int playerLayerIndex;
    
    private bool isPlayerNearby = false;
    private bool isOpened = false;

    private Vector3 basePosition;
    private Quaternion baseRotation;
    private Vector3 baseScale;

    void Awake()
    {
        mainCamera = Camera.main;
        
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;
        baseScale = transform.localScale;
    }

    void Start()
    {
        playerLayerIndex = LayerMask.NameToLayer(playerLayerName);

        if (promptCanvas != null)
        {
            promptCanvas.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isOpened) return;

        if (other.gameObject.layer == playerLayerIndex)
        {
            isPlayerNearby = true;
            if (promptCanvas != null) promptCanvas.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isOpened) return;

        if (other.gameObject.layer == playerLayerIndex)
        {
            isPlayerNearby = false;
            if (promptCanvas != null) promptCanvas.SetActive(false);
        }
    }

    void Update()
    {
        if (isOpened) return;

        if (isPlayerNearby)
        {
            if (promptCanvas != null && promptCanvas.activeSelf && mainCamera != null)
            {
                promptCanvas.transform.rotation = Quaternion.LookRotation(
                    promptCanvas.transform.position - mainCamera.transform.position
                );
            }

            float bounceOffset = Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed)) * bounceHeight;
            float wiggleOffset = Mathf.Sin(Time.time * shakeSpeed) * shakeAngle;

            transform.localPosition = basePosition + new Vector3(0f, bounceOffset, 0f);
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, wiggleOffset);

            // 3. New Input System check for the interaction key
            if (Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
            {
                OpenChest();
            }
        }
        else
        {
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

            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

            float easeOut = Mathf.Sin(normalizedTime * Mathf.PI * 0.5f);
            transform.localPosition = Vector3.Lerp(startPos, targetPos, easeOut);

            transform.localScale = Vector3.Lerp(baseScale, Vector3.zero, normalizedTime);

            yield return null;
        }

        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }
}