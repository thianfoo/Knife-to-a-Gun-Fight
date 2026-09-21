using UnityEngine;

public class BulletMark : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damageAmount = 25;

    private EnemyAI attachedEnemy;

    private void OnTriggerEnter(Collider other)
    {
        // Check if we hit an enemy
        if (other.CompareTag("Enemy"))
        {
            EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
            if (enemy != null)
            {
                // 1. Physically stick the mark to the hit bone/collider
                transform.SetParent(other.transform);

                // 2. Register this mark with the enemy so it can be cleaned up on death
                attachedEnemy = enemy;
                attachedEnemy.RegisterBulletMark(this);

                // 3. Apply the damage!
                enemy.TakeDamage(damageAmount);
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