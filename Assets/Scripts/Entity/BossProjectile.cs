using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    Transform target;
    EntityHealth targetHealth;
    EntityHealth owner;
    Vector2 velocity;
    Vector2 destination;
    float damage;
    float lifetime;
    float hitRadius;
    float homingStrength;
    float explosionRadius;
    bool explodeAtDestination;

    static Sprite projectileSprite;

    public static BossProjectile Spawn(
        Vector2 position,
        Vector2 velocity,
        Transform target,
        EntityHealth targetHealth,
        EntityHealth owner,
        float damage,
        float lifetime,
        float hitRadius,
        Color color,
        Vector2 scale,
        float homingStrength = 0f)
    {
        GameObject projectileObject = new GameObject("Boss Projectile");
        projectileObject.transform.position = position;
        const float projectileVisualMultiplier = 2.4f;
        projectileObject.transform.localScale = scale * projectileVisualMultiplier;

        SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetArtSprite(color, scale);
        renderer.color = renderer.sprite == null ? color : Color.white;
        renderer.sortingOrder = 20;

        BossProjectile projectile = projectileObject.AddComponent<BossProjectile>();
        projectile.velocity = velocity;
        projectile.target = target;
        projectile.targetHealth = targetHealth;
        projectile.owner = owner;
        projectile.damage = damage;
        projectile.lifetime = lifetime;
        projectile.hitRadius = hitRadius;
        projectile.homingStrength = homingStrength;
        return projectile;
    }

    public void ExplodeAt(Vector2 point, float radius)
    {
        destination = point;
        explosionRadius = radius;
        explodeAtDestination = true;
    }

    void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            if (explodeAtDestination)
                Explode();
            else
                Destroy(gameObject);
            return;
        }

        if (target != null && homingStrength > 0f)
        {
            Vector2 desired = ((Vector2)target.position - (Vector2)transform.position).normalized * velocity.magnitude;
            velocity = Vector2.Lerp(velocity, desired, homingStrength * Time.deltaTime);
        }

        transform.position += (Vector3)(velocity * Time.deltaTime);

        if (explodeAtDestination && Vector2.Distance(transform.position, destination) <= Mathf.Max(0.15f, velocity.magnitude * Time.deltaTime))
        {
            Explode();
            return;
        }

        if (target != null && targetHealth != null && !targetHealth.isDeath &&
            Vector2.Distance(transform.position, target.position) <= hitRadius)
        {
            targetHealth.GetDamage(damage, owner);
            Destroy(gameObject);
        }
    }

    void Explode()
    {
        enabled = false;
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sprite = BossArt.GetEffect(6);
            renderer.color = Color.white;
        }
        transform.localScale = Vector3.one * explosionRadius * 1.15f;
        if (target != null && targetHealth != null && !targetHealth.isDeath &&
            Vector2.Distance(transform.position, target.position) <= explosionRadius)
        {
            targetHealth.GetDamage(damage, owner);
        }
        Destroy(gameObject, 0.08f);
    }

    static Sprite GetProjectileSprite()
    {
        if (projectileSprite == null)
        {
            projectileSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                1f
            );
        }
        return projectileSprite;
    }

    static Sprite GetArtSprite(Color color, Vector2 scale)
    {
        int index;
        if (color.b > 0.7f && color.r < 0.5f)
            index = 3;
        else if (color.b > 0.7f && color.r > 0.5f)
            index = 7;
        else if (color.g > 0.65f && color.r > 0.65f)
            index = 1;
        else if (color.r > 0.8f && color.g > 0.2f)
            index = 5;
        else if (scale.x > 1f)
            index = 4;
        else
            index = 0;

        return BossArt.GetEffect(index) ?? GetProjectileSprite();
    }
}
