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
        // 지면에서 벗어나 하강하기 시작하면 크라우칭 해제 후 낙하
        if (player.ShouldFall)
            player.Fsm.ChangeState(player.FallState);
    }
}
