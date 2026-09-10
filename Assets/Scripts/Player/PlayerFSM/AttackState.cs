using UnityEngine;

public class AttackState : PlayerState
{
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AttackCountHash = Animator.StringToHash("AttackCount");
    private int attackCount = 0;

    public AttackState(Player player) : base(player) { }

    public override void Enter()
    {
        player.ResetAttackEnd();
        player.Animator.SetInteger(AttackCountHash, attackCount);
        player.Animator.SetTrigger(AttackHash);
    }

    public override void Exit()
    {
        attackCount = 0;
        player.Animator.SetInteger(AttackCountHash, attackCount);
        player.AttackEnd();
        player.CommandQueue.Clear();
        player.CloseInputQueue();
        player.Animator.ResetTrigger(AttackHash);
    }

    public override void Update()
    {
        if (attackCount > player.Data.MaxAttackCount)
            player.Fsm.ChangeState(player.IdleState);

        if (player.IsAttackEnd)
        {
            if (player.CommandQueue.Count == 0)
            {
                player.Fsm.ChangeState(player.IdleState);
                return;
            }

            if (player.CommandQueue.Dequeue() == PlayerCommand.Attack)
            {
                attackCount++;
                player.Animator.SetInteger(AttackCountHash, attackCount);
                player.Animator.SetTrigger(AttackHash);
                player.CommandQueue.Clear();
                player.ResetAttackEnd();
                return;
            }
        }
    }
}
