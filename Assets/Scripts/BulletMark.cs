using UnityEngine;

public class BulletMark : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damageAmount = 25;
    public int weakZoneMultiplier = 3;

    private EnemyAI attachedEnemy;

    private void OnTriggerEnter(Collider other)
    {
        // Ensure the hit object or its root is tagged Enemy
        if (other.CompareTag("Enemy"))
        {
            EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
            if (enemy != null)
            {
                // Stick mark to the hit weak zone bone/collider
                transform.SetParent(other.transform);

                attachedEnemy = enemy;
                attachedEnemy.RegisterBulletMark(this);

                // Calculate damage
                int finalDamage = damageAmount;
                if (enemy.CheckIfWeakZone(other))
                {
                    finalDamage *= weakZoneMultiplier;
                    Debug.Log($"CRITICAL WEAK ZONE HIT on {other.gameObject.name}!");
                }

                enemy.TakeDamage(finalDamage);
            }
        }
    }

    public void DeactivateMark()
    {
        // Unparent before deactivating so it doesn't get messed up if pooled
        transform.SetParent(null);
        gameObject.SetActive(false);
    }
}