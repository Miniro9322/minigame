public class IdleState : PlayerState
{
    public IdleState(Player player) : base(player) { }

    public override void FixedUpdate()
    {
        if (player.ShouldFall)
            player.Fsm.ChangeState(player.FallState);
    }
}
