using UnityEngine;

public class BossRushAttack : BossState
{
    private static readonly int RushHash = Animator.StringToHash("Rush");

    private readonly float rushAmount = 10f;
    private readonly float rushDuration = 1f;

    private Vector3 startPoint;
    private float rushTime = 0f;
    private Vector3 rushVector;
    private float dir;

    public BossRushAttack(Boss1 boss) : base(boss) { }

    public override void Enter()
    {
        boss.Animator.Play(RushHash);
        boss.CanParry = true;
        startPoint = boss.transform.position;
        dir = boss.transform.localScale.x;
        float targetX = Mathf.Clamp(startPoint.x + rushAmount * dir, -17f, 19f);
        rushVector = new Vector3(targetX, startPoint.y);
        boss.DisableRush();
    }

    public override void Exit()
    {
        boss.IsAttack = false;
        rushTime = 0f;
        boss.DisableRush();
    }

    public override void Update()
    {
        if (boss.CurrHp <= 0f)
            boss.Fsm.ChangeState(boss.Death);

        if (!boss.CanRush) return;

        rushTime += Time.deltaTime;
        boss.transform.position = Vector3.Lerp(startPoint, rushVector, rushTime / rushDuration);

        if (!boss.IsAttack)
            boss.Fsm.ChangeState(boss.Idle);
    }
}
