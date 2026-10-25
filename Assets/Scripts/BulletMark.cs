using UnityEngine;

public class BulletMark : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damageAmount = 25;
    public int weakZoneMultiplier = 3;

    private EnemyAI attachedEnemy;
    private BossAI attachedBoss;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            // 1. Check Standard Enemy
            EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
            if (enemy != null)
            {
                transform.SetParent(other.transform);
                attachedEnemy = enemy;
                attachedEnemy.RegisterBulletMark(this);

                enemy.SpawnHitEffect(transform.position, transform.rotation, other.transform);

                int finalDamage = damageAmount;
                if (enemy.CheckIfWeakZone(other))
                {
                    finalDamage *= weakZoneMultiplier;
                }

                enemy.TakeDamage(finalDamage);
                return;
            }

            // 2. Check Boss AI
            BossAI boss = other.GetComponentInParent<BossAI>();
            if (boss != null)
            {
                transform.SetParent(other.transform);
                attachedBoss = boss;
                attachedBoss.RegisterBulletMark(this);

                int finalDamage = damageAmount;
                if (boss.CheckIfWeakZone(other))
                {
                    finalDamage *= weakZoneMultiplier;
                }

                boss.TakeDamage(finalDamage);
                return;
            }
        }
    }

    public void DeactivateMark()
    {
        transform.SetParent(null);
        gameObject.SetActive(false);
    }
}