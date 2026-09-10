using UnityEngine;

public class BossChase : BossState
{
    private static readonly int MoveHash = Animator.StringToHash("Move");

    public BossChase(Boss1 boss) : base(boss) { }

    public override void Enter()
    {
        boss.Animator.SetBool(MoveHash, true);
    }

    public override void Exit()
    {
        boss.Animator.SetBool(MoveHash, false);
    }

    public override void Update()
    {
        if (boss.CurrHp <= 0)
        {
            boss.Fsm.ChangeState(boss.Death);
            return;
        }

        boss.transform.position += boss.transform.localScale.x * boss.Data.moveSpeed * Time.deltaTime * Vector3.right;

        // 사거리 안에 들어오면 다시 패턴 결정
        if (boss.PlayerDistance <= boss.FarRange)
            boss.Fsm.ChangeState(boss.DecideState);
    }
}
