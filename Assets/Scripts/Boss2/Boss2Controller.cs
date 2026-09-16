using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BehaviorGraphAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class Boss2Controller : MonoBehaviour, IDamageable
{
    private static readonly int DeathHash = Animator.StringToHash("Death");

    [Header("── 스폰 ──")]
    [Tooltip("시작 층 (0=평지, 1=1층, 2=2층, 3=3층)")]
    [SerializeField] private int spawnFloor = 0;
    [Tooltip("시작 위치 (0=왼쪽, 1=중앙, 2=오른쪽)")]
    [SerializeField] private int spawnSide = 1;
    [Tooltip("체크하면 왼쪽/중앙/오른쪽 중 랜덤 스폰")]
    [SerializeField] private bool randomSpawn = false;

    [Header("── 패턴 인터벌 ──")]
    [Tooltip("공격 후 다음 패턴까지 대기 시간")]
    [SerializeField] private int postAttackDelay = 1500;
    [Tooltip("패링 투사체 공격: 텔포 전 대기 (반사 투사체가 보스에 닿을 시간)")]
    [SerializeField] private int parryWindowDelay = 2000;

    [Header("── 텔레포트 ──")]
    [Tooltip("인덱스: 0=평지, 1=1층, 2=2층, 3=3층")]
    [SerializeField] private Transform[] floorLeftPositions;
    [SerializeField] private Transform[] floorCenterPositions;
    [SerializeField] private Transform[] floorRightPositions;
    [SerializeField] private int teleportDuration = 300;

    [Header("── 패링 투사체 ──")]
    [SerializeField] private GameObject parriableProjectilePrefab; // ParriableProjectile 컴포넌트 포함 프리팹
    [SerializeField] private int   projectileCount    = 3;
    [SerializeField] private float projectileSpeed    = 8f;
    [SerializeField] private int projectileInterval = 400;

    [Header("── 불기둥 ──")]
    [SerializeField] private GameObject firePillarWarningPrefab;
    [SerializeField] private GameObject firePillarExplosionPrefab; // FirePillar 컴포넌트 포함 프리팹
    [SerializeField] private int   firePillarCount           = 3;
    [SerializeField] private int firePillarWarningDuration = 2000;
    [SerializeField] private float firePillarGroundY         = 0f;
    [SerializeField] private float firePillarRangeXMin       = -8f;
    [SerializeField] private float firePillarRangeXMax       = 8f;

    [Header("── 탄막 ──")]
    [SerializeField] private GameObject bulletPrefab; // BossBullet 컴포넌트 포함 프리팹
    [SerializeField] private int   bulletCurtainCount = 12;
    [SerializeField] private float bulletSpeed        = 6f;
    [SerializeField] private int bulletLifetime     = 5000;

    [Header("── 층 레이저 (2페이즈) ──")]
    [SerializeField] private GameObject laserPrefab; // FloorLaser 컴포넌트 포함 프리팹
    [SerializeField] private Transform[] floorLaserLeftPositions;
    [SerializeField] private Transform[] floorLaserRightPositions;
    [SerializeField] private int laserWarningDuration = 1500;
    [SerializeField] private int laserActiveDuration  = 2000;

    [Header("── 그로기 패턴 (2페이즈) ──")]
    [SerializeField] private GameObject groggyPartPrefab; // GroggyPart 컴포넌트 포함 프리팹
    [SerializeField] private Transform[] groggyPartSpawnPoints;
    [SerializeField] private Transform   groggyCenterPosition;
    [SerializeField] private float groggyTimeLimit       = 15f;
    [SerializeField] private int groggyDuration        = 3000;
    [SerializeField] private float groggyHPRecoveryAmount = 200f;

    [Header("── 시각 효과 ──")]
    [SerializeField] private Color phase2Color = new(1f, 0.3f, 0.3f);
    private SpriteRenderer spriteRenderer;

    [Header("── 피격 효과 ──")]
    [SerializeField] private int hitFlashDuration = 80;
    [Tooltip("피격 순간 낮출 timeScale 값 (0 에 가까울수록 강하게 멈춘다)")]
    [SerializeField] private float hitStopScale    = 0.05f;
    [Tooltip("히트스탑 지속 시간. unscaled 기준")]
    [SerializeField] private float hitStopDuration  = 0.04f;

    [Header("── 사망 연출 ──")]
    [SerializeField] private int deathStopDuration  = 300;
    [SerializeField] private float deathSlowScale     = 0.2f;
    [SerializeField] private float deathSlowDuration  = 1.0f;

    public UnityEvent OnBossDead;

    public bool  IsActing  { get; internal set; } = false;
    public bool  IsGroggy  { get; internal set; } = false;
    public float CurrentHP { get; private set; }
    public float MaxHP     => maxHP;
    public BossData Data   => data;
    public bool IsDead     => CurrentHP <= 0f;
    public bool IsPhase2   => CurrentHP <= maxHP * 0.5f;
    public float HPPercent => CurrentHP / maxHP;

    private BehaviorGraphAgent behaviorAgent;
    private bool phase2Triggered = false;

    private GameObject player;
    private Animator animator;

    [SerializeField] private BossData    data;
    [SerializeField] private GameObject  parryWarning;
    [SerializeField] private GameObject  avoidWarning;
    [SerializeField] private Transform   lookAtZone;
    [SerializeField] private AudioClip   parryFireballClip;
    [SerializeField] private AudioClip   fireballClip;
    [SerializeField] private AudioClip   ChargeClip;
    [SerializeField] private AudioClip   fireWallClip;
    [SerializeField] private AudioClip   hitClip;
    public Transform LookAtZone => lookAtZone;
    private int maxHP;

    private Boss2PoolManager pools;
    private Boss2TeleportController teleport;
    private Boss2AttackPatterns attacks;
    private Boss2VisualEffects effects;

    private void Awake()
    {
        player    = GameObject.FindGameObjectWithTag("Player");
        maxHP     = data.Hp;
        CurrentHP = maxHP;
        behaviorAgent  = GetComponent<BehaviorGraphAgent>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator       = GetComponent<Animator>();
        parryWarning.SetActive(false);
        avoidWarning.SetActive(false);

        pools = new Boss2PoolManager(
            bulletPrefab, firePillarExplosionPrefab, firePillarWarningPrefab,
            laserPrefab, groggyPartPrefab, parriableProjectilePrefab);

        teleport = new Boss2TeleportController(
            transform, spriteRenderer, teleportDuration,
            floorLeftPositions, floorCenterPositions, floorRightPositions,
            initialFloor: 0, initialSide: 1);

        attacks = new Boss2AttackPatterns(
            this, pools, teleport, transform, animator, player ? player.transform : null, data,
            postAttackDelay, parryWindowDelay,
            new ParryProjectileConfig(parriableProjectilePrefab, projectileCount, projectileSpeed, projectileInterval),
            new FirePillarConfig(firePillarWarningPrefab, firePillarExplosionPrefab, firePillarCount,
                firePillarWarningDuration, firePillarGroundY, firePillarRangeXMin, firePillarRangeXMax),
            new BulletCurtainConfig(bulletPrefab, bulletCurtainCount, bulletSpeed, bulletLifetime),
            new FloorLaserConfig(laserPrefab, floorLaserLeftPositions, floorLaserRightPositions,
                laserWarningDuration, laserActiveDuration),
            new GroggyPatternConfig(groggyPartPrefab, groggyPartSpawnPoints, groggyCenterPosition,
                groggyTimeLimit, groggyDuration, groggyHPRecoveryAmount, ChargeClip));

        effects = new Boss2VisualEffects(
            spriteRenderer, phase2Color, () => IsPhase2,
            hitFlashDuration, deathStopDuration, deathSlowScale, deathSlowDuration);
    }

    private void Start()
    {
        int floor = Mathf.Clamp(spawnFloor, 0, 3);
        int side  = randomSpawn ? UnityEngine.Random.Range(0, 3) : Mathf.Clamp(spawnSide, 0, 2);

        Vector3 spawnPos = teleport.GetPosition(floor, side);
        if (spawnPos != Vector3.zero)
        {
            transform.position = spawnPos;
            teleport.SetCurrentPosition(floor, side);
        }
    }

    private void Update()
    {
        if (IsDead) return;

        if (player != null)
        {
            float diff   = player.transform.position.x - transform.position.x;
            float facing = transform.localScale.x;
            float absX   = Mathf.Abs(facing);

            if      (facing > 0f && diff < -0.5f)
                transform.localScale = new Vector3(-absX, transform.localScale.y, transform.localScale.z);
            else if (facing < 0f && diff >  0.5f)
                transform.localScale = new Vector3( absX, transform.localScale.y, transform.localScale.z);
        }

        if (!phase2Triggered && IsPhase2 && !IsDead)
        {
            phase2Triggered = true;
            OnPhase2Start();
        }
    }

    public IDamageable.DamageInfo SetDamage() => default;
    public void GetDamage(IDamageable.DamageInfo damageInfo) => TakeDamage(damageInfo.damage);

    public void TakeDamage(int damage)
    {
        if (IsDead) return;
        SoundManager.Instance.PlaySFX(hitClip);
        CurrentHP = Mathf.Max(0f, CurrentHP - damage);

        if (!IsDead)
        {
            _ = effects.HitFlash();
            _ = TimeControl.HitStop(hitStopScale, hitStopDuration);
        }
        else
        {
            behaviorAgent.BlackboardReference.SetVariableValue("IsDead", true);
            // 그로기 차지 등 루프 사운드가 죽는 순간 즉시 멎도록 (Behavior 그래프가
            // 실행 중인 액션을 곧바로 중단하지 않으므로 여기서 직접 정지)
            SoundManager.Instance.StopSFXLoop();
            if (spriteRenderer) spriteRenderer.enabled = true;
            _ = effects.PlayDeathEffect(OnDeath);
        }
    }

    public void HealHP(float amount) => CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);

    private void OnPhase2Start()
    {
        behaviorAgent.BlackboardReference.SetVariableValue("IsPhase2", true);
        if (spriteRenderer) spriteRenderer.color = phase2Color;
    }

    private void OnDeath()
    {
        behaviorAgent.BlackboardReference.SetVariableValue("IsDead", true);
        pools.DestroyAllActive();
        attacks.ClearSpawnedGroggyParts();
        animator.Play(DeathHash);
        _ = WaitForDeathAnimation();
    }

    async UniTask WaitForDeathAnimation()
    {
        await UniTask.Yield(PlayerLoopTiming.LastUpdate);
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Death"))
            await UniTask.Yield(PlayerLoopTiming.LastUpdate);
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            await UniTask.Yield(PlayerLoopTiming.LastUpdate);

        OnBossDead?.Invoke();
        await UniTask.Delay(500);
        Destroy(gameObject);
    }

    public UniTask AttackParriableProjectile(Action<bool> callback, CancellationTokenSource cts)
        => attacks.AttackParriableProjectile(callback, cts);

    public UniTask AttackFirePillar(Action<bool> callback, CancellationTokenSource cts)
        => attacks.AttackFirePillar(callback, cts);

    public UniTask AttackBulletCurtain(Action<bool> callback, CancellationTokenSource cts)
        => attacks.AttackBulletCurtain(callback, cts);

    public UniTask AttackFloorLaser(Action<bool> callback, CancellationTokenSource cts)
        => attacks.AttackFloorLaser(callback, cts);

    public UniTask AttackGroggyPattern(Action<bool> callback, CancellationTokenSource cts)
        => attacks.AttackGroggyPattern(callback, cts);

    public void OnFireSignal() => attacks.OnFireSignal();

    public void OnGroggyPartDestroyed() => attacks.OnGroggyPartDestroyed();

    private void DestroyIt()        => Destroy(gameObject);
    private void EnableWarning()    => parryWarning.SetActive(true);
    private void DisableWarning()   => parryWarning.SetActive(false);
    private void EnableAvoidWarning()  => avoidWarning.SetActive(true);
    private void DisableAvoidWarning() => avoidWarning.SetActive(false);

    public void OnGameOver()
        => behaviorAgent.BlackboardReference.SetVariableValue("IsGameOver", true);

    private void PlayParryFireballSound() => SoundManager.Instance.PlaySFX(parryFireballClip);
    private void PlayFireballSound()      => SoundManager.Instance.PlaySFX(fireballClip);
    private void PlayFireWallSound()      => SoundManager.Instance.PlaySFX(fireWallClip);
}
