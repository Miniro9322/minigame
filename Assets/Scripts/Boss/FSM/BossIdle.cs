using UnityEngine;

public class BossIdle : BossState
{
    private readonly float maxThinkTime = 2f;
    private float thinkTime;
    private float thinkDuration;

    public BossIdle(Boss1 boss) : base(boss) { }

    public override void Enter()
    {
        thinkDuration = Random.Range(1f, maxThinkTime);
    }

    public override void Exit()
    {
        thinkTime = 0f;
    }

    public override void Update()
    {
        if (boss.CurrHp <= 0)
        {
            boss.Fsm.ChangeState(boss.Death);
            return;
        }

        thinkTime += Time.deltaTime;
        if (thinkTime >= thinkDuration)
            boss.Fsm.ChangeState(boss.DecideState);
    }
}
