using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MarsFPSKit
{
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(AudioSource))]
    public class Crate : MonoBehaviour
    {
    [Header("Detection Settings")]
    [Tooltip("The layer name your player is on")]
    public string playerLayerName = "PlayerRoot";

    [Header("Input System (Scriptable Object)")]
    public InputActionReference interactAction;

    [Header("Observer Pattern / Scoring")]
    public ScoreEventChannel scoreEventChannel;
    public int crateScoreValue = 50;

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

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip landSound;

    [Header("Barrel Drop Settings")]
    [Tooltip("Check this if this crate starts in the sky and drops when a barrel explodes")]
    public bool waitForBarrelExplosion = false;
    [Tooltip("The barrel GameObject to monitor. When it disables, the drop triggers.")]
    public GameObject watchedBarrel;
    [Tooltip("Height above ground where the crate starts hanging")]
    public float dropHeightOffset = 15f;
    [Tooltip("Simulated downward gravity speed")]
    public float fallSpeed = 18f;
    [Tooltip("Bounciness upon hitting the ground")]
    public float impactBounceHeight = 0.4f;
    private Kit_ExplodeableBarrel cachedBarrel;

    // Internal state
    private Camera mainCamera;
    private int playerLayerIndex;
    private bool isPlayerNearby = false;
    private bool isOpened = false;
    private bool isReadyToInteract = true;

    private Vector3 groundPosition;
    private Quaternion baseRotation;
    private Vector3 baseScale;
    private BoxCollider boxCollider;

    void Awake()
{
    mainCamera = Camera.main;
    boxCollider = GetComponent<BoxCollider>();
    if (audioSource == null) audioSource = GetComponent<AudioSource>();

    // Cache baseline ground transforms in world space
    groundPosition = transform.position;
    baseRotation = transform.rotation;
    baseScale = transform.localScale;

    if (waitForBarrelExplosion)
    {
        isReadyToInteract = false;
        boxCollider.enabled = false;
        // Position directly up in world space
        transform.position = groundPosition + new Vector3(0f, dropHeightOffset, 0f);
    }
}

    void Start()
    {
        playerLayerIndex = LayerMask.NameToLayer(playerLayerName);
        if (promptCanvas != null) promptCanvas.SetActive(false);

        if (waitForBarrelExplosion && watchedBarrel != null)
        {
            StartCoroutine(WaitForBarrelRoutine());
        }
    }

    void OnEnable()
    {
        if (interactAction != null)
        {
            if (!interactAction.action.enabled)
            {
                interactAction.action.Enable();
            }
            interactAction.action.performed += OnInteractInput;
        }
    }

    void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed -= OnInteractInput;
        }
    }

    // Monitors the barrel until wasDestroyed becomes true
    private IEnumerator WaitForBarrelRoutine()
    {
        // If the barrel starts disabled, wait until it becomes active or assigned
        while (watchedBarrel == null)
        {
            yield return null;
        }

        // Cache the component once
        cachedBarrel = watchedBarrel.GetComponent<Kit_ExplodeableBarrel>();
        while (cachedBarrel == null)
        {
            cachedBarrel = watchedBarrel.GetComponent<Kit_ExplodeableBarrel>();
            yield return null;
        }

        // Wait WHILE the barrel has NOT been destroyed yet
        while (watchedBarrel != null && !cachedBarrel.wasDestroyed)
        {
            yield return null;
        }

        // Trigger drop fall sequence once wasDestroyed becomes true
        yield return StartCoroutine(FallToGroundRoutine());

        // Enable trigger and allow player interactions
        boxCollider.enabled = true;
        isReadyToInteract = true;
    }

    private IEnumerator FallToGroundRoutine()
    {
        float currentY = transform.position.y;

        // Downward fall loop
        while (currentY > groundPosition.y)
        {
            currentY -= fallSpeed * Time.deltaTime;
            if (currentY < groundPosition.y) currentY = groundPosition.y;

            transform.position = new Vector3(groundPosition.x, currentY, groundPosition.z);
            yield return null;
        }

        transform.position = groundPosition;

        if (audioSource != null && landSound != null)
        {
            audioSource.PlayOneShot(landSound);
        }

        // Ground impact bounce
        float elapsed = 0f;
        float bounceDuration = 0.25f;
        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;
            float bounceY = Mathf.Sin((elapsed / bounceDuration) * Mathf.PI) * impactBounceHeight;
            transform.position = groundPosition + new Vector3(0f, bounceY, 0f);
            yield return null;
        }

        transform.position = groundPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isOpened || !isReadyToInteract) return;

        if (other.gameObject.layer == playerLayerIndex)
        {
            isPlayerNearby = true;
            if (promptCanvas != null) promptCanvas.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isOpened || !isReadyToInteract) return;

        if (other.gameObject.layer == playerLayerIndex)
        {
            isPlayerNearby = false;
            if (promptCanvas != null) promptCanvas.SetActive(false);
        }
    }

    private void OnInteractInput(InputAction.CallbackContext context)
    {
        if (isPlayerNearby && !isOpened && isReadyToInteract)
        {
            OpenChest();
        }
    }

    void Update()
    {
        if (isOpened || !isReadyToInteract) return;

        if (isPlayerNearby)
        {
            float bounceOffset = Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed)) * bounceHeight;
            float wiggleOffset = Mathf.Sin(Time.time * shakeSpeed) * shakeAngle;

            // Use transform.position to match groundPosition
            transform.position = groundPosition + new Vector3(0f, bounceOffset, 0f);
            transform.rotation = baseRotation * Quaternion.Euler(0f, 0f, wiggleOffset);
        }
        else
        {
            // Use transform.position to match groundPosition
            if (transform.position != groundPosition || transform.rotation != baseRotation)
            {
                transform.position = Vector3.Lerp(transform.position, groundPosition, Time.deltaTime * 5f);
                transform.rotation = Quaternion.Slerp(transform.rotation, baseRotation, Time.deltaTime * 5f);
            }
        }
    }

    void OpenChest()
    {
        isOpened = true;

        if (promptCanvas != null) promptCanvas.SetActive(false);

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
        Vector3 targetPos = groundPosition + new Vector3(0f, flyUpwardDistance, 0f);

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / openDuration;

            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

            float easeOut = Mathf.Sin(normalizedTime * Mathf.PI * 0.5f);
            transform.localPosition = Vector3.Lerp(groundPosition, targetPos, easeOut);
            transform.localScale = Vector3.Lerp(baseScale, Vector3.zero, normalizedTime);

            yield return null;
        }

        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }
}
}