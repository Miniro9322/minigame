public class IdleState : PlayerState
{
    public IdleState(Player player) : base(player) { }

    public override void FixedUpdate()
    {
        if (!player.Grounded && player.Rb.linearVelocity.y < 0f)
            player.Fsm.ChangeState(player.FallState);
    }
}
