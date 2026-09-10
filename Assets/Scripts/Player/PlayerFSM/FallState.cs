using UnityEngine;

public class FallState : PlayerState
{
    private static readonly int FallHash = Animator.StringToHash("Fall");

    public FallState(Player player) : base(player) { }

    public override void Enter()
    {
        player.Animator.SetBool(FallHash, true);
        player.Animator.Play(FallHash);
    }

    public override void Exit()
    {
        player.Animator.SetBool(FallHash, false);
    }

    public override void FixedUpdate()
    {
        if (player.Rb.linearVelocity.y < 0f)
        {
            player.Rb.linearVelocity += (player.Data.FallMultiplier - 1f)
                * Physics2D.gravity.y * Time.fixedDeltaTime * Vector2.up;
        }

        if (player.Grounded)
        {
            player.PlayLandSound();
            player.Fsm.ChangeState(player.IdleState);
        }
    }
}
