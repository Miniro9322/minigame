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

    public override void FixedUpdate()
    {
        // 공중에서 패링한 경우 하강 시작하면 낙하로 전이
        if (player.ShouldFall)
            player.Fsm.ChangeState(player.FallState);
    }

    public override void Update()
    {
        parryTime += Time.deltaTime;

        if (parryTime > player.Data.ParryInterval)
            player.ChangeToNeutralState();
    }
}
