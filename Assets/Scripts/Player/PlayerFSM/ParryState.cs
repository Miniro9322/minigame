using UnityEngine;

public class ParryState : PlayerState
{
    private static readonly int ParryHash = Animator.StringToHash("Parrying");
    private float parryTime;

    public ParryState(Player player) : base(player) { }

    public override void Enter()
    {
        player.Animator.Play(ParryHash);
        parryTime = 0f;
        player.ParryStart?.Invoke();
        player.ToggleParry();
    }

    public override void Exit()
    {
        player.ToggleParry();
    }

    public override void Update()
    {
        parryTime += Time.deltaTime;

        if (parryTime > player.Data.ParryInterval)
            player.Fsm.ChangeState(player.IdleState);
    }
}
