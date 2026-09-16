using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class TimeControl
{
    private static int generation;

    public static int Claim(float scale)
    {
        int token = ++generation;
        Time.timeScale = scale;
        return token;
    }

    public static void Set(int token, float scale)
    {
        if (token == generation) Time.timeScale = scale;
    }

    public static void Release(int token)
    {
        if (token == generation) Time.timeScale = 1f;
    }

    public static bool IsOwner(int token) => token == generation;

    public static async UniTask HitStop(float scale, float unscaledDuration)
    {
        int token = Claim(scale);
        await UniTask.Delay(TimeSpan.FromSeconds(unscaledDuration), ignoreTimeScale: true);
        Release(token);
    }

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
