using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    public EntityHealth health;
    public PlayerStat stat;
    public float direction;

    public Rigidbody2D rigid;

    [SerializeField] LayerMask groundMask_;
    [SerializeField] float groundDist_ = 0.5f;

    [SerializeField] private LayerMask enermymask;

    public float atkCool;
    void Start()
    {
        health = GetComponent<EntityHealth>();
        stat = GetComponent<PlayerStat>();
        rigid = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (atkCool > 0)
        {
            atkCool -= Time.deltaTime * (1 + stat.GetResultValue("atkSpeed") / 100);
        }
        mobUpdate();
    }

    protected virtual void mobUpdate() {}

    public void Chase(Transform target)
    {
        if (target.position.x > transform.position.x)
        {
            move(Vector2.right);
        }
        else
        {
            move(Vector2.left);
        }
    }

     public void move(Vector2 axis)
    {
        float moaveSpeed = stat.GetResultValue("moveSpeed");
        transform.Translate(axis.normalized * moaveSpeed * Time.deltaTime);
    }

    public void Setvelocity(Vector2 dir)
    {
        rigid.linearVelocity = dir;
    }
    // Update is called once per frame

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
       Gizmos.DrawWireCube(center: (Vector2)transform.position + range.offset, range.size);
    }
    void OnDrawGizmos()
    {
        DrawGizmos();
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position + Vector3.down * groundDist_ * 0.5f, new Vector3(0.3f, groundDist_));
    }

    public void Attack(float cool, AttackRange range, Vector2 center)
    {

        if (atkCool > 0)
            return;
        atkCool = cool;
        var col = Physics2D.OverlapBoxAll(center + range.offset, range.size, 0, enermymask);
        foreach(var target in col)
        {
            EntityHealth hp = target.GetComponent<EntityHealth>();
            if (hp != null)
            {
                hp.GetDamage(stat.GetResultValue("attackDamage"), health);
            }
        }

    
    }

    protected virtual void DrawGizmos()
    {
        
    }


    
}
