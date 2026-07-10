using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerInput : MonoBehaviour
{

    private PlayerMovement movement;
    private PlayerBattle battle;
    public Vector2 axis;


    void Awake(){
        movement = GetComponent<PlayerMovement>();
        battle = GetComponent<PlayerBattle>();
    }

   public void OnMove(InputValue value)
   {
        Vector2 axis_ = value.Get<Vector2>();

        axis = new Vector2(axis_.x, 0);
   }

   public bool HasAxis()
   {
        return axis.x != 0 || axis.y != 0;
   }

   public void OnJump(){
        movement.Jump();
   }

   public void OnAttack()
     {
          battle.Attack();
     }
}