using UnityEngine;

public class Boss : Enemy
{

    [SerializeField]
    PlayerController player;
    public float attackDistance = 1.5f;

    [SerializeField] AttackRange defaultAttack;

    // Update is called once per frame


    protected override void mobUpdate()
    {

        if (atkCool > 0)
        {
            atkCool -= Time.deltaTime * (1 + stat.GetResultValue("atkSpeed") / 100);
        }

        if(Vector2.Distance(player.transform.position, transform.position) <= attackDistance)
        {
            Attack(0.5f, defaultAttack, transform.position);
        }
        else
        {
            Chase(player.transform);
        }
    }

    protected override void DrawGizmos()
    {
        Draw(defaultAttack);
    }
}
