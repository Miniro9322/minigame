using UnityEngine;

public class HitState : PlayerState
{
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly Color32 blinkColor = new(255, 180, 180, 255);

    private Color defaultColor;
    private float blinkTime;
    private float blinkDuration;
    private bool isBlinked;

    public HitState(Player player) : base(player) { }

    public override void Enter()
    {
        defaultColor = player.Sr.color;

        player.Animator.Play(HitHash);

        blinkTime = player.Data.BlinkInterval;
        blinkDuration = player.Data.BlinkDurationInterval;
        isBlinked = false;

        player.ToggleInvincible();
        ApplyKnockback();
    }

    private void ApplyKnockback()
    {
        // knockbackDir 이 zero 면 플레이어가 바라보는 반대 방향으로 날림
        Vector2 dir = player.KnockbackDir;
        float xSign = dir == Vector2.zero ? -player.transform.localScale.x : Mathf.Sign(dir.x);

        player.Rb.linearVelocity = new Vector2(
            xSign * player.Data.KnockbackForceX,
            player.Data.KnockbackForceY
        );
    }

    public override void Exit()
    {
        player.Sr.color = defaultColor;
        player.ResetAttackEnd();
        player.ToggleInvincible();
    }

    public override void Update()
    {
        if (blinkTime > 0f)
        {
            if (blinkDuration > 0f)
            {
                blinkDuration -= Time.deltaTime;
            }
            else
            {
                isBlinked = !isBlinked;
                player.Sr.color = isBlinked ? blinkColor : defaultColor;
                blinkDuration = player.Data.BlinkDurationInterval;
            }

            blinkTime -= Time.deltaTime;
        }
        else
        {
            player.Sr.color = defaultColor;

            // 피격 넉백은 위로 뜰 수 있어 Jump/Fall 을 속도로 구분 (일반 종료와 다름)
            if (!player.Grounded)
            {
                if (player.Rb.linearVelocity.y > 0f)
                    player.Fsm.ChangeState(player.JumpState);
                else
                    player.Fsm.ChangeState(player.FallState);
            }
            else
            {
                player.Fsm.ChangeState(player.IdleState);
            }
        }
    }
}
