using UnityEngine;

public class BossAttack2 : BossState
{
    private static readonly int Attack2Hash = Animator.StringToHash("Attack2");

    public BossAttack2(Boss1 boss) : base(boss) { }

    public override void Enter()
    {
        boss.Animator.Play(Attack2Hash);
        boss.CanParry = false;
        boss.IgnoreInvincible = true;
    }

    public override void Exit()
    {
        boss.IsAttack = false;
        boss.IgnoreInvincible = false;
    }

    public override void Update()
    {
        if (boss.CurrHp <= 0f)
            boss.Fsm.ChangeState(boss.Death);

        if (!boss.IsAttack)
            boss.Fsm.ChangeState(boss.Idle);
    }
}
