using UnityEngine;


[System.Serializable]
   
   public struct AttackRange{
        public Vector2 offset, size;

        public bool drawGizmos;
    }

public class PlayerBattle : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public EntityHealth health;
    public PlayerStat stat;
    public float atkCool;
    
    public AttackRange defaultAttack;
    [SerializeField] private LayerMask enermymask;
    void Start()
    {
        health = GetComponent<EntityHealth>();
        stat = GetComponent<PlayerStat>();
    }

    void Update()
    {
        if (atkCool > 0)
        {
            atkCool -= Time.deltaTime * (1 + stat.GetResultValue("atkSpeed") / 100);
        }
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
