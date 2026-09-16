using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>보스2의 피격/사망 연출(스프라이트 플래시, 타임슬로우)을 전담한다.</summary>
public class Boss2VisualEffects
{
    private readonly SpriteRenderer spriteRenderer;
    private readonly Color phase2Color;
    private readonly Func<bool> isPhase2;

    private readonly int hitFlashDuration;

    private readonly int deathStopDuration;
    private readonly float deathSlowScale;
    private readonly float deathSlowDuration;

    public Boss2VisualEffects(
        SpriteRenderer spriteRenderer,
        Color phase2Color,
        Func<bool> isPhase2,
        int hitFlashDuration,
        int deathStopDuration,
        float deathSlowScale,
        float deathSlowDuration)
    {
        this.spriteRenderer   = spriteRenderer;
        this.phase2Color      = phase2Color;
        this.isPhase2         = isPhase2;
        this.hitFlashDuration = hitFlashDuration;
        this.deathStopDuration = deathStopDuration;
        this.deathSlowScale    = deathSlowScale;
        this.deathSlowDuration = deathSlowDuration;
    }

    public async UniTask HitFlash()
    {
        if (!spriteRenderer) return;
        Color baseColor = isPhase2() ? phase2Color : Color.white;
        spriteRenderer.color = Color.black;
        await UniTask.Delay(hitFlashDuration, ignoreTimeScale: true);
        spriteRenderer.color = baseColor;
    }

    public async UniTask PlayDeathEffect(Action onComplete)
    {
        int token = TimeControl.Claim(0f);
        await UniTask.Delay(deathStopDuration, ignoreTimeScale: true);
        if (!TimeControl.IsOwner(token)) { onComplete(); return; }

        TimeControl.Set(token, deathSlowScale);
        float elapsed = 0f;
        while (elapsed < deathSlowDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            await UniTask.Yield(PlayerLoopTiming.LastUpdate);
        }

        TimeControl.Release(token);
        onComplete();
    }
}
