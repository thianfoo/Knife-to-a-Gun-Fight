using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    public float damageAmount = 25f;

    void Update()
    {
        // Despawn if it falls below the map
        if (transform.position.y < -25f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 2. Check if we hit the player
        // (Adjust the tag check based on what your player collider is tagged)
        if (other.CompareTag("Player") || other.gameObject.layer == LayerMask.NameToLayer("PlayerRoot"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TakePlayerDamage(damageAmount);
            }
        }
    }
}