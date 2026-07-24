using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public PlayerMovement movement;
    public PlayerAnimator animator;
    public PlayerInput input;
    PlayerBattle battle;
    void Start()
    {
        movement = GetComponent<PlayerMovement>();
        input = GetComponent<PlayerInput>();
        animator = GetComponent<PlayerAnimator>();
        battle = GetComponent<PlayerBattle>();
    }

    void Update()
    {
        EntityHealth health = GetComponent<EntityHealth>();
        if ((health != null && health.IsStunned) || (battle != null && battle.IsReloading))
        {
            movement.Move(Vector2.zero);
            if (animator != null) animator.SetMoving(false, Vector2.zero);
            return;
        }
        movement.Move(input.axis);

        if (animator != null)
            animator.SetMoving(input.HasAxis() && !movement.IsDashing, input.axis);
    }
}
