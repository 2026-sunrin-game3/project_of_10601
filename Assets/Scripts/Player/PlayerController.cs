using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public PlayerMovement movement;
    public PlayerAnimator animator;
    public PlayerInput input;
    void Start()
    {
        movement = GetComponent<PlayerMovement>();
        input = GetComponent<PlayerInput>();
        animator = GetComponent<PlayerAnimator>();
    }

    void Update()
    {
        movement.move(input.axis);

        animator.SetMoving(input.HasAxis(), input.axis);
    }
}
