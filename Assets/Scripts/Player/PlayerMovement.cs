using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PlayerStat))]
[DisallowMultipleComponent]
public class PlayerMovement : MonoBehaviour
{
    Rigidbody2D rigid;
    Collider2D bodyCollider;
    public PlayerStat Stat;

    [Header("Run")]
    [SerializeField] float groundAcceleration = 70f;
    [SerializeField] float groundDeceleration = 90f;
    [SerializeField, Range(0f, 1f)] float airControl = 0.65f;

    [Header("Jump")]
    public float jumpPower = 12f;
    [SerializeField] float maxFallSpeed = 22f;
    [SerializeField, Range(0f, 1f)] float jumpCutMultiplier = 0.45f;
    [SerializeField] float coyoteTime = 0.12f;
    [SerializeField] float jumpBufferTime = 0.12f;
    [SerializeField] LayerMask groundMask_;
    [SerializeField] float groundDist_ = 0.1f;

    [Header("Dash")]
    [SerializeField] float dashSpeed = 18f;
    [SerializeField] float dashDuration = 0.16f;
    [SerializeField] float dashCooldown = 0.55f;

    float moveInput;
    float coyoteTimer;
    float jumpBufferTimer;
    float dashTimer;
    float dashCooldownTimer;
    float dashDirection = 1f;
    Vector2 standingColliderSize;
    Vector2 standingColliderOffset;

    public bool IsGrounded { get; private set; }
    public bool IsDashing => dashTimer > 0f;
    public bool IsCrouching { get; private set; }
    public bool ActionLocked { get; set; }
    public float VerticalVelocity => rigid == null ? 0f : rigid.linearVelocity.y;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        Stat = GetComponent<PlayerStat>();
        if (bodyCollider is BoxCollider2D box)
        {
            standingColliderSize = box.size;
            standingColliderOffset = box.offset;
        }
    }

    void Update()
    {
        IsGrounded = CheckGrounded();
        coyoteTimer = IsGrounded ? coyoteTime : Mathf.Max(0f, coyoteTimer - Time.deltaTime);
        jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - Time.deltaTime);
        dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - Time.deltaTime);

        if (jumpBufferTimer > 0f && coyoteTimer > 0f && !IsDashing)
            PerformJump();
    }

    void FixedUpdate()
    {
        if (ActionLocked)
        {
            rigid.linearVelocity = Vector2.zero;
            return;
        }
        if (IsDashing)
        {
            rigid.linearVelocity = Vector2.right * dashDirection * dashSpeed;
            dashTimer = Mathf.Max(0f, dashTimer - Time.fixedDeltaTime);
            return;
        }

        float targetSpeed = IsCrouching ? 0f : moveInput * Stat.GetResultValue("moveSpeed");
        float control = IsGrounded ? 1f : airControl;
        float acceleration = Mathf.Abs(targetSpeed) > 0.01f ? groundAcceleration : groundDeceleration;
        float nextX = Mathf.MoveTowards(rigid.linearVelocity.x, targetSpeed,
            acceleration * control * Time.fixedDeltaTime);
        float nextY = Mathf.Max(rigid.linearVelocity.y, -maxFallSpeed);
        rigid.linearVelocity = new Vector2(nextX, nextY);
    }

    public void Move(Vector2 axis)
    {
        moveInput = ActionLocked ? 0f : Mathf.Clamp(axis.x, -1f, 1f);
    }

    // Kept for existing callers while movement is now applied in FixedUpdate.
    public void move(Vector2 axis) => Move(axis);

    public void RequestJump()
    {
        jumpBufferTimer = jumpBufferTime;
        if (coyoteTimer > 0f && !IsDashing)
            PerformJump();
    }

    public bool Jump()
    {
        if (ActionLocked) return false;
        bool canJump = (IsGrounded || coyoteTimer > 0f) && !IsDashing;
        RequestJump();
        return canJump;
    }

    public bool FlipJump()
    {
        if (ActionLocked) return false;
        bool canJump = (IsGrounded || coyoteTimer > 0f) && !IsDashing;
        if (!canJump) return false;
        SetCrouching(false);
        PerformJump();
        rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, jumpPower * 1.12f);
        return true;
    }

    public void SetCrouching(bool crouching)
    {
        IsCrouching = crouching && !IsDashing;
        if (bodyCollider is BoxCollider2D box && standingColliderSize != Vector2.zero)
        {
            box.size = IsCrouching ? new Vector2(standingColliderSize.x, standingColliderSize.y * .55f) : standingColliderSize;
            box.offset = IsCrouching ? standingColliderOffset + Vector2.down * standingColliderSize.y * .225f : standingColliderOffset;
        }
    }

    public void CutJump()
    {
        if (!IsDashing && rigid.linearVelocity.y > 0f)
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.x,
                rigid.linearVelocity.y * jumpCutMultiplier);
    }

    public bool TryDash(float direction)
    {
        if (ActionLocked || IsDashing || dashCooldownTimer > 0f)
            return false;

        dashDirection = direction == 0f
            ? (Mathf.Abs(moveInput) > 0.01f ? Mathf.Sign(moveInput) : 1f)
            : Mathf.Sign(direction);
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        rigid.linearVelocity = Vector2.right * dashDirection * dashSpeed;
        return true;
    }

    void PerformJump()
    {
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        IsGrounded = false;
        rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, jumpPower);
    }

    public void SetVelocity(Vector2 velocity)
    {
        rigid.linearVelocity = velocity;
    }

    public bool OnGround() => CheckGrounded();

    bool CheckGrounded()
    {
        if (bodyCollider == null)
            return false;

        Bounds bounds = bodyCollider.bounds;
        Vector2 center = new Vector2(bounds.center.x, bounds.min.y - groundDist_ * 0.5f);
        Vector2 size = new Vector2(Mathf.Max(0.1f, bounds.size.x * 0.75f), groundDist_);
        return Physics2D.OverlapBox(center, size, 0f, groundMask_) != null;
    }

    void OnDrawGizmosSelected()
    {
        Collider2D col = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();
        if (col == null) return;
        Bounds bounds = col.bounds;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new Vector2(bounds.center.x, bounds.min.y - groundDist_ * 0.5f),
            new Vector2(Mathf.Max(0.1f, bounds.size.x * 0.75f), groundDist_));
    }
}
