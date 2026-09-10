using UnityEngine;

public class BossDeath : BossState
{
    private static readonly int DeathHash = Animator.StringToHash("Die");

    public BossDeath(Boss1 boss) : base(boss) { }

    public override void Enter()
    {
        boss.Animator.SetTrigger(DeathHash);
        boss.SetDeath();
    }
}
