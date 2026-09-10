using UnityEngine;

public class BossAttack1 : BossState
{
    private static readonly int Attack1Hash = Animator.StringToHash("Attack1");

    public BossAttack1(Boss1 boss) : base(boss) { }

    public override void Enter()
    {
        boss.Animator.Play(Attack1Hash);
        boss.CanParry = false;
    }

    public override void Exit()
    {
        boss.IsAttack = false;
    }

    public override void Update()
    {
        if (boss.CurrHp <= 0f)
            boss.Fsm.ChangeState(boss.Death);

        if (!boss.IsAttack)
            boss.Fsm.ChangeState(boss.Idle);
    }
}
