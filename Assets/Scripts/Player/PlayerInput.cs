using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    PlayerMovement movement;
    PlayerAnimator animator;
    PlayerBattle battle;
    bool secondaryMouseHeld;
    public Vector2 axis;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        battle = GetComponent<PlayerBattle>();
        animator = GetComponent<PlayerAnimator>();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            PlayerAnimator.CombatMotion swordMotion = battle.TrySwordPlant(Facing());
            animator?.PlayMotion(swordMotion);
        }

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && battle.CurrentAmmo < battle.MaxAmmo)
        {
            if (movement.IsCrouching)
            {
                movement.SetCrouching(false);
                animator?.SetCrouching(false);
            }
            battle.TryStartReload();
        }

        if (Mouse.current != null)
        {
            bool rightButtonHeld = Mouse.current.rightButton.isPressed;
            if (rightButtonHeld != secondaryMouseHeld)
            {
                secondaryMouseHeld = rightButtonHeld;
                HandleSecondaryAttack(rightButtonHeld);
            }
        }

        // SendMessages can miss a canceled callback when action maps change.
        // Synchronizing with the physical key guarantees that releasing S stands up.
        if (Keyboard.current != null)
        {
            bool shouldCrouch = !battle.IsReloading && !battle.IsSwordPlanted && Keyboard.current.sKey.isPressed;
            if (movement.IsCrouching != shouldCrouch)
            {
                movement.SetCrouching(shouldCrouch);
                animator?.SetCrouching(shouldCrouch);
            }
        }
    }

    public void OnMove(InputValue value) { Vector2 input = value.Get<Vector2>(); axis = new Vector2(input.x, 0f); }
    public bool HasAxis() => Mathf.Abs(axis.x) > .01f;

    public void OnJump(InputValue value)
    {
        if (battle.IsReloading) return;
        if (value.isPressed)
        {
            if (movement.IsCrouching)
            {
                if (movement.FlipJump()) animator?.PlayMotion(PlayerAnimator.CombatMotion.Flip);
            }
            else if (movement.Jump()) animator?.Jump();
        }
        else movement.CutJump();
    }

    public void OnCrouch(InputValue value)
    {
        if (battle.IsReloading) return;
        movement.SetCrouching(value.isPressed);
        animator?.SetCrouching(value.isPressed);
    }

    public void OnBasicAttack()
    {
        if (battle.IsReloading) return;
        if (battle.IsSwordPlanted)
        {
            battle.ReleaseSwordPlant(false);
            return;
        }
        PlayerAnimator.CombatMotion motion = battle.ContextualAttack(Facing());
        animator?.PlayMotion(motion);
    }

    public void OnAttack1()
    {
        if (battle.IsReloading) return;
        if (battle.LaunchAttack(Facing())) animator?.PlayMotion(PlayerAnimator.CombatMotion.Launch);
    }

    public void OnSkill1()
    {
        if (battle.IsReloading) return;
        if (battle.GunPlant(Facing())) animator?.PlayMotion(PlayerAnimator.CombatMotion.GunPlant);
    }

    public void OnSecondaryAttack(InputValue value)
    {
        // Mouse input is polled in Update so the release event cannot be lost.
        // Keep this callback as a fallback for any future non-mouse binding.
        if (Mouse.current != null) return;
        HandleSecondaryAttack(value.isPressed);
    }

    void HandleSecondaryAttack(bool held)
    {
        if (battle.IsReloading) return;
        Vector2 aimWorldPosition = MouseAimWorldPosition();
        if (!battle.IsSwordPlanted)
            animator?.FaceDirection(aimWorldPosition.x - transform.position.x);
        PlayerAnimator.CombatMotion motion = battle.SetSecondaryHeld(held, aimWorldPosition);
        animator?.PlayMotion(motion);
    }

    public void OnDash() => battle.Dash(Facing());
    int Facing() => (int)(animator != null ? animator.direction : (axis.x == 0f ? 1f : Mathf.Sign(axis.x)));

    Vector2 MouseAimWorldPosition()
    {
        Camera camera = Camera.main;
        if (camera == null || Mouse.current == null)
            return (Vector2)transform.position + Vector2.right * Facing();

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 screenPosition = new Vector3(mousePosition.x, mousePosition.y,
            Mathf.Abs(camera.transform.position.z - transform.position.z));
        return camera.ScreenToWorldPoint(screenPosition);
    }
}
