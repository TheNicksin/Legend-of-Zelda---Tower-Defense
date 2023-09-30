using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerAnimationManager : NetworkBehaviour
{

    [SerializeField] Animator playerAnim;

    private int currentState;

    #region Animation States

    //int PLAYER_ANIM = Animator.StringToHash("player_Anim");
    int PLAYER_IDLE = Animator.StringToHash("Idle");
    int PLAYER_WALKING = Animator.StringToHash("Walking");
    int PLAYER_RUNNING = Animator.StringToHash("Running");
    int PLAYER_FLOSSDANCE = Animator.StringToHash("Flossing");
    int PLAYER_MIDAIRJUMP = Animator.StringToHash("Falling");
    int PLAYER_LANDINGJUMP = Animator.StringToHash("Landing Jump");
    int PLAYER_HANDWAVE = Animator.StringToHash("Hand Wave");
    int PLAYER_CROUCHIDLE = Animator.StringToHash("Crouch Idle");
    int PLAYER_CROUCHWALK = Animator.StringToHash("Crouch Walking");

    #endregion
    bool emoting = false;
    //bool walking = false;

    #region Emote Animations

    public void FlossingAnimation()
    {
        //if (walking != false) return;

        ChangeAnimationState(PLAYER_FLOSSDANCE, 0.2f);
        emoting = true;
    }

    public void HandWaveAnimation()
    {
        //if (walking != false) return;

        ChangeAnimationState(PLAYER_HANDWAVE, 0.2f);
        emoting = true;
    }

    public void StopEmoting() { emoting = false; }
    #endregion

    public void IdleAnimation()
    {
        if (emoting == false)
            ChangeAnimationState(PLAYER_IDLE, 0.2f);

        //walking = false;
    }

    public void WalkingAnimation()
    {
        ChangeAnimationState(PLAYER_WALKING, 0.2f);
        emoting = false;
        //walking = true;
    }

    public void RunningAnimation()
    {
        ChangeAnimationState(PLAYER_RUNNING, 0.2f);
    }

    public void MidAirJumpAnimation()
    {
        ChangeAnimationState(PLAYER_MIDAIRJUMP, 0.07f);
        //emoting = false;
    }

    public void LandingJumpAnimation()
    {
        ChangeAnimationState(PLAYER_LANDINGJUMP, 0.1f);
        //emoting = true;
    }

    public void CrouchIdleAnimation()
    {
        ChangeAnimationState(PLAYER_CROUCHIDLE, 0.1f);
    }

    public void CrouchWalkingAnimation()
    {
        ChangeAnimationState(PLAYER_CROUCHWALK, 0.1f);
    }

    public void ChangeAnimationState(int newState)
    {
        // stops the same animation from interrupting itself
        if (currentState == newState) return;

        // plays the animation
        playerAnim.Play(newState);
        CmdAnimatorPlay(newState, 0f);

        //reassigns the current state
        currentState = newState;
    }

    
    // TRANSITION METHOD
    public void ChangeAnimationState(int newState, float transitionTime)
    {
        // stops the same animation from interrupting itself
        if (currentState == newState) return;

        // plays the animation / transitionTime determines how long the transition will take
        playerAnim.CrossFadeInFixedTime(newState, transitionTime);
        CmdAnimatorPlay(newState, transitionTime);

        //reassigns the current state
        currentState = newState;
    }


    [Command]
    void CmdAnimatorPlay(int newState, float transitionTime)
    {
        RpcAnimatorPlay(newState, transitionTime);
    }

    [ClientRpc]
    void RpcAnimatorPlay(int newState, float transitionTime)
    {
        if (isLocalPlayer) return;

        playerAnim.CrossFadeInFixedTime(newState, transitionTime);
    }
}