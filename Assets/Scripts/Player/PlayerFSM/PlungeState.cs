using UnityEngine;

public class PlungeState : PlayerState
{
    private static readonly int PlungeHash = Animator.StringToHash("Plunge");
    private static readonly int IdleHash = Animator.StringToHash("Idle");

    public PlungeState(Player player) : base(player) { }

    public override void Enter()
    {
        player.Animator.Play(PlungeHash);
        player.Rb.linearVelocity = new Vector2(player.Rb.linearVelocity.x, -player.Data.PlungeSpeed);
        player.downAttackZone.gameObject.SetActive(true);
    }

    public override void Exit()
    {
        player.Animator.Play(IdleHash);
        player.downAttackZone.gameObject.SetActive(false);
    }

    public override void FixedUpdate()
    {
        if (player.Grounded)
            player.Fsm.ChangeState(player.IdleState);
    }
}
