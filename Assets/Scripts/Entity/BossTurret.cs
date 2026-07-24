using UnityEngine;

public class BossTurret : MonoBehaviour
{
    Transform target;
    EntityHealth targetHealth;
    EntityHealth owner;
    float damage;
    float duration;
    float fireInterval;
    float projectileSpeed;
    float fireTimer;

    public static void Spawn(Vector2 position, Transform target, EntityHealth targetHealth, EntityHealth owner,
        float damage, float duration, float fireInterval, float projectileSpeed)
    {
        GameObject turretObject = new GameObject("Boss Turret");
        turretObject.transform.position = position;
        turretObject.transform.localScale = new Vector3(0.75f, 1.15f, 1f);

        SpriteRenderer renderer = turretObject.AddComponent<SpriteRenderer>();
        renderer.sprite = BossArt.GetEffect(2);
        renderer.color = renderer.sprite == null ? new Color(0.55f, 0.05f, 0.8f) : Color.white;
        renderer.sortingOrder = 10;

        BossTurret turret = turretObject.AddComponent<BossTurret>();
        turret.target = target;
        turret.targetHealth = targetHealth;
        turret.owner = owner;
        turret.damage = damage;
        turret.duration = duration;
        turret.fireInterval = fireInterval;
        turret.projectileSpeed = projectileSpeed;
    }

    void Update()
    {
        duration -= Time.deltaTime;
        fireTimer -= Time.deltaTime;
        if (duration <= 0f || target == null || targetHealth == null || targetHealth.isDeath)
        {
            Destroy(gameObject);
            return;
        }

        if (fireTimer <= 0f)
        {
            fireTimer = fireInterval;
            Vector2 direction = (target.position - transform.position).normalized;
            BossProjectile.Spawn(transform.position, direction * projectileSpeed, target, targetHealth, owner,
                damage, 4f, 0.35f, new Color(0.8f, 0.15f, 1f), Vector2.one * 0.22f);
        }
    }
}
