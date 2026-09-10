using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Time.timeScale 을 여러 곳(히트스톱, 사망 연출 등)에서 건드릴 때 서로 덮어쓰는 경쟁을 막는 헬퍼.
/// 가장 마지막에 시작한 연출만 timeScale 을 되돌릴 수 있다.
///
/// 저수준: Claim → (Set) → Release 토큰 패턴.
/// 고수준: HitStop / DeathSequence 를 그대로 호출.
/// </summary>
public static class TimeControl
{
    private static int generation;

    /// <summary>timeScale 소유권을 잡고 scale 로 설정. 반환된 토큰을 Set/Release 에 전달한다.</summary>
    public static int Claim(float scale)
    {
        int token = ++generation;
        Time.timeScale = scale;
        return token;
    }

    /// <summary>내가 아직 소유자일 때만 scale 변경</summary>
    public static void Set(int token, float scale)
    {
        if (token == generation) Time.timeScale = scale;
    }

    /// <summary>내가 아직 소유자일 때만 1f 로 복원</summary>
    public static void Release(int token)
    {
        if (token == generation) Time.timeScale = 1f;
    }

    public static bool IsOwner(int token) => token == generation;

    /// <summary>scale 로 만든 뒤 unscaled 기준 duration(초) 대기 후 복원. 도중 더 최근 호출이 있으면 양보.</summary>
    public static async UniTask HitStop(float scale, float unscaledDuration)
    {
        int token = Claim(scale);
        await UniTask.Delay(TimeSpan.FromSeconds(unscaledDuration), ignoreTimeScale: true);
        Release(token);
    }

    /// <summary>완전 정지(stopMs) → 슬로우모션(slowScale, slowDuration 초) → 복원.</summary>
    public static async UniTask DeathSequence(int stopMs, float slowScale, float slowDuration)
    {
        int token = Claim(0f);
        await UniTask.Delay(stopMs, ignoreTimeScale: true);
        if (!IsOwner(token)) return;

        Set(token, slowScale);
        float elapsed = 0f;
        while (elapsed < slowDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            await UniTask.Yield(PlayerLoopTiming.LastUpdate);
        }
        Release(token);
    }
}
