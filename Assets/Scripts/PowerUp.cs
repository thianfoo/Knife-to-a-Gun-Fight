using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum PowerUpType { Goggles, WeakZone }
    public PowerUpType powerUpType;
    public float duration = 15f; // How long the power-up lasts

    [Header("Drop Animation")]
    public float floatSpeed = 2f;
    public float rotationSpeed = 90f;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Simple floating and spinning animation
        float newY = startPos.y + (Mathf.Sin(Time.time * floatSpeed) * 0.2f);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("PlayerRoot"))
        {
            if (GameManager.Instance != null)
            {
                if (powerUpType == PowerUpType.Goggles)
                    GameManager.Instance.ActivateGoggles(duration);
                else if (powerUpType == PowerUpType.WeakZone)
                    GameManager.Instance.ActivateWeakZones(duration);
            }
            
            // Add a pickup sound here if desired before destroying
            Destroy(gameObject);
        }
    }
}