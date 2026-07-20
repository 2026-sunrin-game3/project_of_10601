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
        indicator.indicateDamage(ctx.damage, transform.position + new Vector3(Random.Range(-0.3f, 0.3f), 1), Color.orange);
    }

    void Update()
    {
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
        Vector2 center = transform.position + Vector3.down * groundDist_ * 0.5f;
        Vector2 size = new Vector3(0.3f, groundDist_);
        Collider2D[] cast = Physics2D.OverlapBoxAll(center, size, 0f, groundMask_);

        return cast.Length > 0;
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