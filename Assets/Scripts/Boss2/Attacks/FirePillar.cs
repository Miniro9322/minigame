using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;

public class FirePillar : MonoBehaviour, IDamageable
{
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private ParticleSystem particle;
    [SerializeField] private AudioClip exploseAudio;
    private int damage;

    private bool hasHit = false;

    private IObjectPool<FirePillar> objectPool;
    public IObjectPool<FirePillar> ObjectPool { set => objectPool = value; }

    public void Init(int baseAtk) => damage = Mathf.RoundToInt(baseAtk * damageMultiplier);

    public IDamageable.DamageInfo SetDamage() => new() { damage = damage, canParry = false };

    public void GetDamage(IDamageable.DamageInfo damageInfo) { }

    // Start() 제거 — 풀에서 꺼낼 때 재실행이 안 되므로 Setup()으로 대체

    /// <summary>풀에서 꺼낸 직후 Boss2Controller가 명시적으로 호출합니다.</summary>
    public void Setup()
    {
        hasHit = false;
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play();
        SoundManager.Instance.PlaySFX(exploseAudio);
        _ = WaitParticle();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        if (other.CompareTag("Player"))
        {
            other.GetComponent<IDamageable>()?.GetDamage(SetDamage());
            hasHit = true;
        }
    }

    private async UniTask WaitParticle()
    {
        await UniTask.WaitUntil(() => !particle.IsAlive());
        if (gameObject.activeSelf)
            objectPool.Release(this);
    }
}
