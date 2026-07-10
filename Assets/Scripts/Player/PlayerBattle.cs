using UnityEngine;

public class PlayerBattle : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public EntityHealth health;
    public PlayerStat stat;
    [System.Serializable]
   
   public struct AttackRange{
        public Vector2 offset, size;

        public bool drawGizmos;
    }

    public AttackRange defaultAttack;
    [SerializeField] private LayerMask enermymask;
    void Start()
    {
        health = GetComponent<EntityHealth>();
        stat = GetComponent<PlayerStat>();
    }
    public void Attack()
    {
        var col = Physics2D.OverlapBoxAll((Vector2)transform.position + defaultAttack.offset, defaultAttack.size, 0, enermymask);
        foreach(var target in col)
        {
            EntityHealth hp = target.GetComponent<EntityHealth>();
            if (hp != null)
            {
                hp.GetDamage(stat.GetREsultValue("attackDamage"), health);
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
