using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    public EntityHealth health;
    public PlayerStat stat;
    public Rigidbody2D rigid;
    public float direction;
    [SerializeField] LayerMask groundMask_;
    [SerializeField] float groundDist_ = 0.5f;
    public float atkCool;
    [SerializeField] LayerMask enemyMask;
    [SerializeField] DamageIndicator indicator;
    void Start()
    {
        health = GetComponent<EntityHealth>();
        stat = GetComponent<PlayerStat>();
        rigid = GetComponent<Rigidbody2D>();

        health.Ondamage(OnHurt);
        health.OnDeath(OnDeath);
    }

    void OnDeath(EntityHealth.Context ctx)
    {
        Destroy(gameObject);
    }

    void OnHurt(EntityHealth.Context ctx)
    {
        Color color = ctx.isCritical ? Color.yellow : Color.orange;
        indicator.indicateDamage(ctx.damage, transform.position + new Vector3(Random.Range(-0.3f, 0.3f), 1), color,
            ctx.isCritical ? 1.55f : 1f);
    }

    void Update()
    {
        if (health != null && health.IsStunned)
        {
            if (rigid != null) rigid.linearVelocity = Vector2.zero;
            return;
        }
        if (atkCool > 0)
            atkCool -= Time.deltaTime * (1 + stat.GetResultValue("atkSpeed") / 100);
            
        MobUpdate();
    }
    protected virtual void MobUpdate(){}

    public void Chase(Transform target)
    {
        if (target.position.x > transform.position.x)
        {
            Move(Vector2.right);
        } else if (target.position.x < transform.position.x)
        {
            Move(Vector2.left);
        }
    }

    public void Move(Vector2 axis)
    {
        float moveSpeed = stat.GetResultValue("moveSpeed");
        transform.Translate(axis.normalized * moveSpeed * Time.deltaTime);
    }
    public void SetVelocity(Vector2 dir)
    {
        rigid.linearVelocity = dir;
    }

    public void Attack(float cool, AttackRange range, Vector2 center)
    {
        if (atkCool > 0)
            return;
        atkCool = cool;

        var col = Physics2D.OverlapBoxAll(center + range.offset,
            range.size,
            0,
            enemyMask
        );

        foreach (var target in col)
        {
            EntityHealth hp = target.GetComponent<EntityHealth>();
            if (hp != null)
            {
                hp.GetDamage(stat.GetResultValue("attackDamage"), health);
            }
        }
    }

    public bool OnGround()
    {
        Collider2D bodyCollider = GetComponent<Collider2D>();
        if (bodyCollider != null)
        {
            if (bodyCollider.IsTouchingLayers(groundMask_)) return true;

            Bounds bounds = bodyCollider.bounds;
            Vector2 center = new Vector2(bounds.center.x, bounds.min.y - .04f);
            Vector2 size = new Vector2(Mathf.Max(.25f, bounds.size.x * .65f), .12f);
            Collider2D[] contacts = Physics2D.OverlapBoxAll(center, size, 0f, groundMask_);
            foreach (Collider2D contact in contacts)
                if (contact != null && contact != bodyCollider && !contact.isTrigger) return true;
            return false;
        }

        Vector2 fallbackCenter = transform.position + Vector3.down * groundDist_ * .5f;
        return Physics2D.OverlapBox(fallbackCenter, new Vector2(.3f, groundDist_), 0f, groundMask_) != null;
    }

    protected void Draw(AttackRange range)
    {
        if (!range.drawGizmos)
            return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((Vector2)transform.position + range.offset, range.size);
    }
    void OnDrawGizmos()
    {
        DrawGizmos();
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position + Vector3.down * groundDist_ * 0.5f, new Vector3(0.3f, groundDist_));
    }

    protected virtual void DrawGizmos() {}
}
