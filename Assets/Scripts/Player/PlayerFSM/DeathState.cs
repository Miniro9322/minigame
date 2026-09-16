using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class DeathState : PlayerState
{
    private static readonly int DieHash = Animator.StringToHash("Die");

    public DeathState(Player player) : base(player) { }

    public override void Enter()
    {
        player.Animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        player.Animator.SetTrigger(DieHash);
        InputSystem.actions.FindActionMap("Player").Disable();
        player.ToggleInvincible();
        _ = DeathSequence();
    }

    private async UniTask DeathSequence()
    {
        // 히트스탑
        int token = TimeControl.Claim(0f);
        await UniTask.Delay(TimeSpan.FromSeconds(player.Data.DeathStopDuration), ignoreTimeScale: true);

        // 슬로우모션으로 사망 애니메이션 재생
        TimeControl.Set(token, player.Data.DeathSlowScale);
        await UniTask.Yield(PlayerLoopTiming.LastUpdate);

        // unscaled 기준으로 애니메이션 길이만큼 대기
        float length = player.Animator.GetCurrentAnimatorStateInfo(0).length;
        await UniTask.Delay(TimeSpan.FromSeconds(length), ignoreTimeScale: true);

        TimeControl.Release(token);
        player.Animator.updateMode = AnimatorUpdateMode.Normal;

        player.OnGameOver?.Invoke();
    }
}
