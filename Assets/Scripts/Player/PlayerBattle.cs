using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UIElements.Experimental;

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

    [SerializeField] LayerMask enermymask;
    [SerializeField] float dashPower, dashTime;
    [SerializeField] DamageIndicator indicator;
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

    public void Attack()
    {

        if (atkCool > 0)
            return;
        atkCool = 0.5f;
        var col = Physics2D.OverlapBoxAll((Vector2)transform.position + defaultAttack.offset, defaultAttack.size, 0, enermymask);
        foreach(var target in col)
        {
            EntityHealth hp = target.GetComponent<EntityHealth>();
            if (hp != null)
            {
                hp.GetDamage(stat.GetResultValue("attackDamage"), health);
            }
        }
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
    }

}
