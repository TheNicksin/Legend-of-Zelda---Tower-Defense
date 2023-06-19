using UnityEngine;
using TheNicksin.Inputsystem;

public class PlayerWalkingState : PlayerBaseState
{

    public override void EnterState(PlayerStateManager player)
    {
        Debug.Log($"Entered Walking State");
    }

    public override void UpdateState(PlayerStateManager player)
    {
        if (!player.playerMovement.IsGrounded()) player.SwitchState(player.FallingState);

        IdleDetection(player);
        InputsForState(player);

        player.playerMovement.MovePlayer(this);
        
        player.playerMovement.JustPlayerGravity();
        player.playerAnimations.WalkingAnimation();
    }

    void IdleDetection(PlayerStateManager player)
    {
        var InputMan = player.inputManager;

        float horizontal = InputMan.AxisMovementValue(InputMan.right, InputMan.left);
        float vertical = InputMan.AxisMovementValue(InputMan.forward, InputMan.backward);

        if (new Vector2(horizontal, vertical) == Vector2.zero) player.SwitchState(player.GroundedState);
    }

    void InputsForState(PlayerStateManager player)
    {
        var InputMan = player.inputManager;

        if (Input.GetKey(InputMan.run))
            player.SwitchState(player.RunningState);

        else if (Input.GetKey(InputMan.jump))
        {
            player.playerMovement.PlayerJumped();
            player.SwitchState(player.FallingState);
        }

        else if (Input.GetKey(InputMan.crouch))
            player.SwitchState(player.CrouchState);
    }
}