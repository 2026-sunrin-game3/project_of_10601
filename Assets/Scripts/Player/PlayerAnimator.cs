using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    Animator animator;
    PlayerStat stat;
    public float direction;
    void Start()
    {
        animator = GetComponent<Animator>();
        stat = GetComponent<PlayerStat>();
    }

    public void SetMoving(bool val, Vector2 axis)
    {
        animator.SetBool("isMoving", val);

        float moveRate = stat.GetResultValue("moveSpeed") / stat.GetBaseValue("moveSpeed");

        animator.SetFloat("moveSpeed", moveRate);

        if (val)
        {
            if (axis.x > 0)
                direction = 1;
            else if (axis.x < 0)
                direction = -1;

            transform.localScale = new Vector2(
                Mathf.Abs(transform.localScale.x) * direction,
                transform.localScale.y
            );
        }
    }

    public void Jump()
    {
        animator.SetTrigger("Jump");
    }

    public void Play(string id)
    {
        animator.Play(id);
    }
}
