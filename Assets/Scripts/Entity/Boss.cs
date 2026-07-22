using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Boss : Enemy
{
    [SerializeField]
    PlayerController player;
    public float attackDist = 1.5f;
    [SerializeField] AttackRange defaultAttack;

    public float dashRange = 5f;
    public float dashPower = 12f;
    public float dashDuration = 0.4f;
    public float dashCoolTime = 3f;
    [SerializeField] AttackRange dashAttack;
    float dashCool;

    public float jumpPower = 7f;
    public float fallSpeed = 25f;
    public float jumpCoolTime = 6f;
    [SerializeField] AttackRange jumpAttack;
    float jumpCool;

    [SerializeField] Slider bossbar;

    public float retreatTime = 0.6f;
    float retreatTimer;

    bool inPattern;

    void Awake()
    {
        // 스폰하자마자 패턴이 터지지 않도록 쿨타임을 채워둔 채로 시작
        dashCool = dashCoolTime;
        jumpCool = jumpCoolTime;
    }

    // Update is called once per frame
    protected override void MobUpdate()
    {
        bossbar.value = health.health / health.maxHealth;

        

        if (dashCool > 0)
            dashCool -= Time.deltaTime;
        if (jumpCool > 0)
            jumpCool -= Time.deltaTime;
        if (retreatTimer > 0)
            retreatTimer -= Time.deltaTime;

        if (inPattern)
            return;

        float dist = Vector2.Distance(player.transform.position, transform.position);

        if (retreatTimer > 0)
        {
            // 붙었다 빠지기: 때리고 나서 잠깐 반대 방향으로 물러남
            float away = player.transform.position.x > transform.position.x ? -1 : 1;
            Move(Vector2.right * away);
            return;
        }

        if (dist <= attackDist)
        {
            if (atkCool <= 0)
                retreatTimer = retreatTime;
            Attack(0.5f, defaultAttack, transform.position);
        }
        else if (dist > dashRange && jumpCool <= 0)
        {
            jumpCool = jumpCoolTime;
            StartCoroutine(JumpAttackPattern());
        }
        else if (dist <= dashRange && dashCool <= 0)
        {
            dashCool = dashCoolTime;
            StartCoroutine(DashAttackPattern());
        }
        else
        {
            Chase(player.transform);
        }
    }

    IEnumerator DashAttackPattern()
    {
        inPattern = true;
        direction = player.transform.position.x > transform.position.x ? 1 : -1;
        SetVelocity(Vector2.right * direction * dashPower);

        float t = 0f;
        while (t < dashDuration && Mathf.Abs(player.transform.position.x - transform.position.x) > attackDist)
        {
            t += Time.deltaTime;
            yield return null;
        }

        SetVelocity(Vector2.zero);
        Attack(0f, dashAttack, transform.position);
        inPattern = false;
    }

    IEnumerator JumpAttackPattern()
    {
        inPattern = true;
        SetVelocity(Vector2.up * jumpPower);

        yield return new WaitForSeconds(0.2f);

        while (!OnGround())
        {
            if (Mathf.Abs(player.transform.position.x - transform.position.x) > attackDist)
            {
                direction = player.transform.position.x > transform.position.x ? 1 : -1;
                Move(Vector2.right * direction);
            }

            if (rigid.linearVelocity.y < 0)
                SetVelocity(new Vector2(rigid.linearVelocity.x, -fallSpeed));

            yield return null;
        }

        Attack(0f, jumpAttack, transform.position);
        inPattern = false;
    }

    protected override void DrawGizmos()
    {
        Draw(defaultAttack);
        Draw(dashAttack);
        Draw(jumpAttack);
    }
}