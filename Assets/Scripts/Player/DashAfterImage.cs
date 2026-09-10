using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 대시 잔상 효과 컨트롤러
/// 캐릭터의 SpriteRenderer가 있는 GameObject에 붙여서 사용
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DashAfterImage : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Afterimage Settings")]
    [Tooltip("잔상 생성 간격 (ms)")]
    [SerializeField] private int spawnInterval = 50;

    [Tooltip("잔상이 처음 생성될 때의 알파값 (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float startAlpha = 0.6f;

    [Tooltip("잔상이 완전히 사라지는 데 걸리는 시간 (초)")]
    [SerializeField] private float fadeDuration = 0.4f;

    [Tooltip("잔상 정렬 순서 오프셋 (음수 = 원본 뒤로)")]
    [SerializeField] private int sortingOrderOffset = -1;

    [Header("Pool Settings")]
    [Tooltip("풀 기본 용량")]
    [SerializeField] private int poolDefaultCapacity = 16;

    [Tooltip("풀 최대 크기 (초과분은 반환 시 파괴)")]
    [SerializeField] private int poolMaxSize = 64;

    private bool isSpawning = false;
    private IObjectPool<AfterImagePiece> piecePool;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        piecePool = new ObjectPool<AfterImagePiece>(
            CreatePiece, OnGetPiece, OnReleasePiece, OnDestroyPiece,
            true, poolDefaultCapacity, poolMaxSize);
    }

    private void OnDestroy()
    {
        piecePool?.Clear();
    }

    private AfterImagePiece CreatePiece()
    {
        GameObject ghost = new GameObject("Afterimage");
        AfterImagePiece piece = ghost.AddComponent<AfterImagePiece>();
        piece.ObjectPool = piecePool;
        return piece;
    }

    private void OnGetPiece(AfterImagePiece p) => p.gameObject.SetActive(true);
    private void OnReleasePiece(AfterImagePiece p) => p.gameObject.SetActive(false);
    private void OnDestroyPiece(AfterImagePiece p) { if (p != null) Destroy(p.gameObject); }

    /// <summary>대시 시작 — 멈추라고 할 때까지 계속 잔상 생성</summary>
    public void StartAfterImage()
    {
        if (isSpawning) return;
        isSpawning = true;
        _ = SpawnLoop();
    }

    /// <summary>대시 종료 — 잔상 생성 중단</summary>
    public void StopAfterImage()
    {
        isSpawning = false;
    }

    private async UniTask SpawnLoop()
    {
        while (isSpawning)
        {
            SpawnOne();
            await UniTask.Delay(spawnInterval);
        }
    }

    private void SpawnOne()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        AfterImagePiece piece = piecePool.Get();
        piece.Play(spriteRenderer, startAlpha, fadeDuration, sortingOrderOffset);
    }
}
