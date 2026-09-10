using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 개별 잔상 1개의 페이드아웃 처리
/// DashAfterImage의 오브젝트 풀에서 생성/반환되므로 직접 붙일 필요 없음
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AfterImagePiece : MonoBehaviour
{
    private SpriteRenderer sr;
    private float fadeDuration;
    private float startAlpha;
    private float timer;
    private bool isPlaying;

    private IObjectPool<AfterImagePiece> objectPool;
    public IObjectPool<AfterImagePiece> ObjectPool { set => objectPool = value; }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>풀에서 꺼낸 잔상을 원본 스프라이트 상태로 맞추고 페이드 시작</summary>
    public void Play(SpriteRenderer source, float startAlpha, float fadeDuration, int sortingOrderOffset)
    {
        transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        transform.localScale = source.transform.lossyScale;

        sr.sprite = source.sprite;
        sr.flipX = source.flipX;
        sr.flipY = source.flipY;
        sr.sortingLayerID = source.sortingLayerID;
        sr.sortingOrder = source.sortingOrder + sortingOrderOffset;

        Color c = source.color;
        c.a = startAlpha;
        sr.color = c;

        this.startAlpha = startAlpha;
        this.fadeDuration = Mathf.Max(0.01f, fadeDuration);
        timer = 0f;
        isPlaying = true;
    }

    private void Update()
    {
        if (!isPlaying) return;

        timer += Time.deltaTime;
        float t = timer / fadeDuration;

        Color c = sr.color;
        c.a = Mathf.Lerp(startAlpha, 0f, t);
        sr.color = c;

        if (t >= 1f)
        {
            isPlaying = false;

            if (objectPool != null)
                objectPool.Release(this);
            else
                Destroy(gameObject);
        }
    }
}
