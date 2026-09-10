using UnityEngine;

public class CrouchState : PlayerState
{
    private static readonly int CrouchHash = Animator.StringToHash("Crouch");

    public CrouchState(Player player) : base(player) { }

    public override void Enter()
    {
        player.Animator.SetBool(CrouchHash, true);
        player.SetColliderCrouch(true);
    }

    public override void Exit()
    {
        player.Animator.SetBool(CrouchHash, false);
        player.SetColliderCrouch(false);
    }

    public override void FixedUpdate()
    {
        // 지면에서 벗어나면 크라우칭 해제
        if (!player.Grounded)
            player.Fsm.ChangeState(player.FallState);
    }
}
