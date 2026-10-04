using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; 

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(AudioSource))]
public class Crate : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("The layer name your player is on")]
    public string playerLayerName = "PlayerRoot";

    [Header("Input System (Scriptable Object)")]
    public InputActionReference interactAction; // Drag your Input Action here

    [Header("Observer Pattern / Scoring")]
    public ScoreEventChannel scoreEventChannel; // Drag the Event Channel here
    public int crateScoreValue = 50;

    [Header("UI Prompt")]
    public GameObject promptCanvas;

    [Header("Animations")]
    public float bounceHeight = 0.3f;
    public float bounceSpeed = 6f;
    public float shakeAngle = 8f;
    public float shakeSpeed = 15f;
    public float openDuration = 1.2f;
    public float flyUpwardDistance = 2.5f;
    public float spinSpeed = 1000f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip openSound;

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

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        playerLayerIndex = LayerMask.NameToLayer(playerLayerName);
        if (promptCanvas != null) promptCanvas.SetActive(false);
    }

    void OnEnable()
    {
        // Subscribe to the Input Action event
        if (interactAction != null)
        {
            interactAction.action.Enable();
            interactAction.action.performed += OnInteractInput;
        }
    }

    void OnDisable()
    {
        // Unsubscribe from the Input Action
        if (interactAction != null)
        {
            interactAction.action.performed -= OnInteractInput;
            interactAction.action.Disable();
        }
    }

    // Input System Event Callback
    private void OnInteractInput(InputAction.CallbackContext context)
    {
        if (isPlayerNearby && !isOpened)
        {
            OpenChest();
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
            float bounceOffset = Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed)) * bounceHeight;
            float wiggleOffset = Mathf.Sin(Time.time * shakeSpeed) * shakeAngle;

            transform.localPosition = basePosition + new Vector3(0f, bounceOffset, 0f);
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, wiggleOffset);
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
        if (promptCanvas != null) promptCanvas.SetActive(false);

        // Meaningful use of Observer Pattern: Broadcast that an obstacle was overcome
        if (scoreEventChannel != null)
        {
            scoreEventChannel.RaiseEvent(crateScoreValue);
        }

        if (audioSource != null && openSound != null)
        {
            audioSource.PlayOneShot(openSound);
        }

        StartCoroutine(FlyAndShrinkRoutine());
    }

    IEnumerator FlyAndShrinkRoutine()
    {
        float elapsed = 0f;
        Vector3 targetPos = basePosition + new Vector3(0f, flyUpwardDistance, 0f);

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / openDuration;

            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
            float easeOut = Mathf.Sin(normalizedTime * Mathf.PI * 0.5f);
            transform.localPosition = Vector3.Lerp(basePosition, targetPos, easeOut);
            transform.localScale = Vector3.Lerp(baseScale, Vector3.zero, normalizedTime);

            yield return null;
        }

        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }
}