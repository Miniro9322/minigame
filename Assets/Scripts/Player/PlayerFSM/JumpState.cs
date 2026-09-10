using UnityEngine;

public class JumpState : PlayerState
{
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    public JumpState(Player player) : base(player) { }

    public override void Enter()
    {
        player.Animator.SetBool(JumpHash, true);
        player.Animator.Play(JumpHash);

        player.Rb.linearVelocity = new Vector2(player.Rb.linearVelocity.x, player.Data.JumpPower);
    }

    public override void Exit()
    {
        player.Animator.SetBool(JumpHash, false);
    }

    public override void FixedUpdate()
    {
        if (!player.JumpHeld && player.Rb.linearVelocity.y > 0f)
        {
            player.Rb.linearVelocity += (player.Data.LowJumpMultiplier - 1f)
                * Physics2D.gravity.y * Time.fixedDeltaTime * Vector2.up;
        }

        if (player.Rb.linearVelocity.y <= 0f)
            player.Fsm.ChangeState(player.FallState);
    }
}
