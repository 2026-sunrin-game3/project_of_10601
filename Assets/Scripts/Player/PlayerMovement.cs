using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rigid;
    public PlayerStat Stat;
    public float jumpPower = 12f;
    [SerializeField] LayerMask groundMask_;
    [SerializeField] float groundDist_ = 0.5f;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        Stat = GetComponent<PlayerStat>();
    }

    public void move(Vector2 axis)
    {
        float moaveSpeed = Stat.GetResultValue("moveSpeed");
        transform.Translate(axis.normalized * moaveSpeed * Time.deltaTime);
    }

    public void Setvelocity(Vector2 dir)
    {
        rigid.linearVelocity = dir;
    }

    public bool OnGround()
    {
        Vector2 center = transform.position + Vector3.down * groundDist_ * 0.5f;
        Vector2 size = new Vector3(0.3f, groundDist_);
        Collider2D[] cast = Physics2D.OverlapBoxAll(center, size, 0f, groundMask_);

        return cast.Length > 0;
    }
    public bool Jump()
    {
        if(OnGround())
        {
            Setvelocity(Vector2.up * jumpPower);

            return true;
        }
        return false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position + Vector3.down * groundDist_ * 0.5f, new Vector3(0.3f, groundDist_));
    }
}