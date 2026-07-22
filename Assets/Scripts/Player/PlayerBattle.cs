using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

[System.Serializable]
   
   public struct AttackRange{
        public Vector2 offset, size;

        public bool drawGizmos;
    }
    


public class PlayerBattle : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public EntityHealth health;
    public PlayerMovement movement;
    public PlayerStat stat;
    public float atkCool;
    public bool inDash;
    
    public AttackRange defaultAttack;

    [Header("Launch Attack")]
    [SerializeField] AttackRange launchAttackRange = new AttackRange
    {
        offset = new Vector2(1f, 0.7f),
        size = new Vector2(1.3f, 1.6f),
        drawGizmos = true
    };
    [SerializeField] float launchPower = 10f;
    [SerializeField] float launchAttackCooldown = 0.5f;

    [SerializeField] LayerMask enermymask;
    [SerializeField] float dashPower, dashTime;
    [SerializeField] DamageIndicator indicator;
    [SerializeField] Slider healthbar;
    void Start()
    {
        health = GetComponent<EntityHealth>();
        stat = GetComponent<PlayerStat>();
        movement = GetComponent<PlayerMovement>();

        health.Ondamage(OnHurt);
        
    }

    void OnHurt(EntityHealth.Context ctx)
    {
        if (ctx.canceled)
            return;

        if(inDash)
        ctx.canceled = true;

        indicator.indicateDamage(ctx.damage, transform.position + new Vector3(0, 1), Color.red);
    }

    void Update()
    {
        healthbar.value = health.health / health.maxHealth;
        if (atkCool > 0)
        {
            atkCool -= Time.deltaTime * (1 + stat.GetResultValue("atkSpeed") / 100);
        }
    }

    public void Dash(int direction)
    {
        StartCoroutine(dash_(direction));
    }



    IEnumerator dash_(int direction)
    {
        movement.SetVelocity(Vector2.right * direction * dashPower);
        yield return new WaitForSeconds(dashTime);
        movement.SetVelocity(Vector2.zero);
       
        inDash = false;
    }

    public void Skill1()
    {
        StartCoroutine(Skill1_());

    }

    IEnumerator Skill1_()
    {
        var atkBuf = new PlayerStat.Buf
        {
            Key = "attackDamage",
            mathType = MathType.Increase,
            Value = 60
        };
        var atkspeedBuf = new PlayerStat.Buf
        {
            Key = "attackDamage",
            mathType = MathType.Increase,
            Value = 60
        };

        stat.bufs.Add(atkBuf);
        stat.bufs.Add(atkspeedBuf);
        stat.Calc("attackDamage");
        stat.Calc("atkSpeed");

        yield return new WaitForSeconds(5);

        stat.bufs.Clear();

        stat.bufs.Remove(atkBuf);
        stat.bufs.Remove(atkspeedBuf);

        stat.Calc("attackDamage");
        stat.Calc("atkSpeed");
    }

    public bool BasicAttack(int direction)
    {
        if (atkCool > 0)
            return false;

        atkCool = 0.5f;
        direction = direction == 0 ? 1 : (int)Mathf.Sign(direction);

        Vector2 attackOffset = defaultAttack.offset;
        attackOffset.x *= direction;
        Vector2 attackCenter = (Vector2)transform.position + attackOffset;
        Collider2D[] targets = Physics2D.OverlapBoxAll(attackCenter, defaultAttack.size, 0f, enermymask);
        HashSet<EntityHealth> damagedTargets = new HashSet<EntityHealth>();

        foreach (Collider2D target in targets)
        {
            EntityHealth hp = target.GetComponentInParent<EntityHealth>();
            if (hp != null && damagedTargets.Add(hp))
            {
                hp.GetDamage(stat.GetResultValue("attackDamage"), health);
            }
        }

        return true;
    }

    public bool LaunchAttack(int direction)
    {
        if (atkCool > 0)
            return false;

        atkCool = launchAttackCooldown;
        direction = direction == 0 ? 1 : (int)Mathf.Sign(direction);

        Vector2 attackOffset = launchAttackRange.offset;
        attackOffset.x *= direction;
        Vector2 attackCenter = (Vector2)transform.position + attackOffset;
        Collider2D[] targets = Physics2D.OverlapBoxAll(
            attackCenter,
            launchAttackRange.size,
            0f,
            enermymask
        );
        HashSet<Rigidbody2D> launchedTargets = new HashSet<Rigidbody2D>();

        foreach (Collider2D target in targets)
        {
            Rigidbody2D targetRigid = target.GetComponentInParent<Rigidbody2D>();
            if (targetRigid != null && launchedTargets.Add(targetRigid))
            {
                Vector2 velocity = targetRigid.linearVelocity;
                velocity.y = launchPower;
                targetRigid.linearVelocity = velocity;
            }
        }

        return true;
    }

    void Draw(AttackRange range)
    {
        if (!range.drawGizmos)
            return;
       Gizmos.color = Color.yellow;
       Gizmos.DrawWireCube(center: (Vector2)transform.position + range.offset, range.size);
    }

    void OnDrawGizmos()
    {
        Draw(defaultAttack);
        Draw(launchAttackRange);
    }

}
