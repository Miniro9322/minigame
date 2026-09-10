using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource loopSfxSource;
    [SerializeField] private float crossFadeDuration = 1f;

    private AudioClip targetBGMClip;
    private CancellationTokenSource bgmCts;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource.ignoreListenerPause = true;
        sfxSource.ignoreListenerPause = true;
        if (loopSfxSource) loopSfxSource.ignoreListenerPause = true;

        ApplyVolume();
    }

    private void OnDestroy()
    {
        if (Instance == this) CancelBgmFade();
    }

    private void ApplyVolume()
    {
        bgmSource.volume = SaveManager.Data.bgmVolume;
        sfxSource.volume = SaveManager.Data.sfxVolume;
        if (loopSfxSource) loopSfxSource.volume = SaveManager.Data.sfxVolume;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus || bgmSource == null || targetBGMClip == null) return;

        CancelBgmFade();
        bgmSource.clip = targetBGMClip;  // 목표 클립으로 강제 설정
        bgmSource.Play();
        bgmSource.volume = SaveManager.Data.bgmVolume;
    }

    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource.clip == clip) return;
        targetBGMClip = clip;
        RestartBgmFade(token => CrossFade(clip, token));
    }

    public void StopBGM() => RestartBgmFade(FadeOut);

    // sfxSource.volume 이 마스터 SFX 볼륨이므로 여기서는 상대 배수만 넘긴다 (이중 감쇠 방지)
    public void PlaySFX(AudioClip clip, float multiple = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, multiple);
    }

    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = volume;
        SaveManager.SetBGMVolume(volume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
        if (loopSfxSource) loopSfxSource.volume = volume;
        SaveManager.SetSFXVolume(volume);
    }

    public void PlaySFXLoop(AudioClip clip)
    {
        if (clip == null || loopSfxSource == null) return;
        loopSfxSource.clip = clip;
        loopSfxSource.loop = true;
        loopSfxSource.Play();
    }

    public void StopSFXLoop()
    {
        if (loopSfxSource == null) return;
        loopSfxSource.loop = false;
        loopSfxSource.Stop();
    }

    private void RestartBgmFade(Func<CancellationToken, UniTask> fade)
    {
        CancelBgmFade();
        bgmCts = new CancellationTokenSource();
        fade(bgmCts.Token).Forget();
    }

    private void CancelBgmFade()
    {
        if (bgmCts == null) return;
        bgmCts.Cancel();
        bgmCts.Dispose();
        bgmCts = null;
    }

    private async UniTask CrossFade(AudioClip newClip, CancellationToken token)
    {
        float startVolume = bgmSource.volume;

        // 페이드 아웃
        float elapsed = 0f;
        while (elapsed < crossFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / crossFadeDuration);
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        bgmSource.clip = newClip;
        bgmSource.Play();

        // 페이드 인
        elapsed = 0f;
        while (elapsed < crossFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(0f, startVolume, elapsed / crossFadeDuration);
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        bgmSource.volume = startVolume;
    }

    private async UniTask FadeOut(CancellationToken token)
    {
        float startVolume = bgmSource.volume;
        float elapsed = 0f;
        while (elapsed < crossFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / crossFadeDuration);
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
        bgmSource.Stop();
        bgmSource.volume = startVolume;
    }
}
