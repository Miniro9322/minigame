using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

[RequireComponent(typeof(BehaviorGraphAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class Boss2Controller : MonoBehaviour, IDamageable
{
    private static readonly int ActingHash       = Animator.StringToHash("Acting");
    private static readonly int LaserHash        = Animator.StringToHash("Laser");
    private static readonly int BigAttackHash    = Animator.StringToHash("BigAttack");
    private static readonly int FireBallHash     = Animator.StringToHash("FireBall");
    private static readonly int SpreadFireBallHash = Animator.StringToHash("SpreadFireBall");
    private static readonly int FireWallHash     = Animator.StringToHash("FireWall");
    private static readonly int DeathHash        = Animator.StringToHash("Death");
    private static readonly int StunHash         = Animator.StringToHash("Stun");

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
    [SerializeField] private float hitStopDuration  = 0.04f;

    [Header("── 사망 연출 ──")]
    [SerializeField] private int deathStopDuration  = 300;
    [SerializeField] private float deathSlowScale     = 0.2f;
    [SerializeField] private float deathSlowDuration  = 1.0f;

    public UnityEvent OnBossDead;

    public bool  IsActing  { get; private set; } = false;
    public bool  IsGroggy  { get; private set; } = false;
    public float CurrentHP { get; private set; }
    public float MaxHP     => maxHP;
    public BossData Data   => data;
    public bool IsDead     => CurrentHP <= 0f;
    public bool IsPhase2   => CurrentHP <= maxHP * 0.5f;
    public float HPPercent => CurrentHP / maxHP;

    private BehaviorGraphAgent behaviorAgent;
    private bool phase2Triggered = false;
    private int  currentFloor    = 0;
    private int  currentSide     = 1;
    private Vector3[,] teleportPositions;

    private int  groggyPartsTotal     = 0;
    private int  groggyPartsDestroyed = 0;
    private List<GroggyPart>  spawnedGroggyParts = new();
    private List<GameObject> activePoolObjects = new();

    private bool fireSignalReceived = false;
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

    private IObjectPool<BossBullet>          bulletPool;
    private IObjectPool<FirePillar>          PillarPool;
    private IObjectPool<GameObject>          PillarWarningPool;
    private IObjectPool<FloorLaser>          LaserPool;
    private IObjectPool<GroggyPart>          PartPool;
    private IObjectPool<ParriableProjectile> ParryPool;

    /// <summary>프리팹에서 컴포넌트 T 를 뽑아내는 오브젝트 풀 생성. onCreate 로 풀 역참조 등을 주입한다.</summary>
    private ObjectPool<T> MakeComponentPool<T>(GameObject prefab, Action<T, IObjectPool<T>> onCreate = null)
        where T : Component
    {
        ObjectPool<T> pool = null;
        pool = new ObjectPool<T>(
            createFunc:      () => { var o = Instantiate(prefab).GetComponent<T>(); onCreate?.Invoke(o, pool); return o; },
            actionOnGet:     p => p.gameObject.SetActive(true),
            actionOnRelease: p => { p.gameObject.SetActive(false); activePoolObjects.Remove(p.gameObject); },
            actionOnDestroy: p => { if (p) Destroy(p.gameObject); });
        return pool;
    }

    private ObjectPool<GameObject> MakeGameObjectPool(GameObject prefab)
        => new(
            createFunc:      () => Instantiate(prefab),
            actionOnGet:     p => p.SetActive(true),
            actionOnRelease: p => { p.SetActive(false); activePoolObjects.Remove(p); },
            actionOnDestroy: p => { if (p) Destroy(p); });


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
        SetupTeleportPositions();

        bulletPool        = MakeComponentPool<BossBullet>         (bulletPrefab);
        PillarPool        = MakeComponentPool<FirePillar>          (firePillarExplosionPrefab, (o, p) => o.ObjectPool = p);
        PillarWarningPool = MakeGameObjectPool                     (firePillarWarningPrefab);
        LaserPool         = MakeComponentPool<FloorLaser>          (laserPrefab,               (o, p) => o.ObjectPool = p);
        PartPool          = MakeComponentPool<GroggyPart>          (groggyPartPrefab,          (o, p) => o.ObjectPool = p);
        ParryPool         = MakeComponentPool<ParriableProjectile> (parriableProjectilePrefab, (o, p) => o.ObjectPool = p);
    }

    private void Start()
    {
        int floor = Mathf.Clamp(spawnFloor, 0, 3);
        int side  = randomSpawn ? UnityEngine.Random.Range(0, 3) : Mathf.Clamp(spawnSide, 0, 2);

        Vector3 spawnPos = teleportPositions[floor, side];
        if (spawnPos != Vector3.zero)
        {
            transform.position = spawnPos;
            currentFloor = floor;
            currentSide  = side;
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
            _ = HitFlash();
            _ = TimeControl.HitStop(0.05f, hitStopDuration);
        }
        else
        {
            behaviorAgent.BlackboardReference.SetVariableValue("IsDead", true);
            // 그로기 차지 등 루프 사운드가 죽는 순간 즉시 멎도록 (Behavior 그래프가
            // 실행 중인 액션을 곧바로 중단하지 않으므로 여기서 직접 정지)
            SoundManager.Instance.StopSFXLoop();
            if (spriteRenderer) spriteRenderer.enabled = true;
            _ = DeathEffect();
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
        DestroyAllSpawnedObjects();
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

    private void DestroyAllSpawnedObjects()
    {
        foreach (var obj in activePoolObjects)
            if (obj != null) Destroy(obj);
        activePoolObjects.Clear();

        foreach (var part in spawnedGroggyParts)
            if (part != null) Destroy(part.gameObject);
        spawnedGroggyParts.Clear();

        bulletPool?.Clear();
        PillarPool?.Clear();
        PillarWarningPool?.Clear();
        LaserPool?.Clear();
        PartPool?.Clear();
        ParryPool?.Clear();
    }

    private void SetupTeleportPositions()
    {
        int floorCount = 4;
        teleportPositions = new Vector3[floorCount, 3];

        for (int i = 0; i < floorCount; i++)
        {
            if (i < floorLeftPositions.Length   && floorLeftPositions[i])
                teleportPositions[i, 0] = floorLeftPositions[i].position;
            if (i < floorCenterPositions.Length && floorCenterPositions[i])
                teleportPositions[i, 1] = floorCenterPositions[i].position;
            if (i < floorRightPositions.Length  && floorRightPositions[i])
                teleportPositions[i, 2] = floorRightPositions[i].position;
        }
    }

    async UniTask DoTeleport(Vector3 target)
    {
        if (spriteRenderer) spriteRenderer.enabled = false;
        await UniTask.Delay(teleportDuration / 2);
        transform.position = target;
        await UniTask.Delay(teleportDuration / 2);
        if (spriteRenderer) spriteRenderer.enabled = true;
    }

    async UniTask TeleportToRandomPosition()
    {
        var candidates = new List<(int floor, int side)>();
        for (int f = 0; f < 4; f++)
            for (int s = 0; s < 3; s++)
                if (teleportPositions[f, s] != Vector3.zero)
                    candidates.Add((f, s));

        candidates.RemoveAll(c => c.floor == currentFloor && c.side == currentSide);
        if (candidates.Count == 0) return;

        var pick = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        await DoTeleport(teleportPositions[pick.floor, pick.side]);
        currentFloor = pick.floor;
        currentSide = pick.side;
    }

    public async UniTask AttackParriableProjectile(Action<bool> callback, CancellationTokenSource cts)
    {
        try
        {
            IsActing = true;
            var playerTf = player?.transform;

            fireSignalReceived = false;

            animator.Play(FireBallHash);
            
            await UniTask.WaitUntil(() => fireSignalReceived);

            cts.Token.ThrowIfCancellationRequested();

            for (int i = 0; i < projectileCount; i++)
            {
                cts.Token.ThrowIfCancellationRequested();

                if (parriableProjectilePrefab && playerTf)
                {
                    Vector3 dir = (playerTf.position - transform.position).normalized;
                    float spread = UnityEngine.Random.Range(-10f, 10f) * Mathf.Deg2Rad;
                    Vector2 fd = new Vector2(
                        dir.x * Mathf.Cos(spread) - dir.y * Mathf.Sin(spread),
                        dir.x * Mathf.Sin(spread) + dir.y * Mathf.Cos(spread)).normalized;

                    var go = ParryPool.Get();
                    go.transform.position = transform.position;
                    activePoolObjects.Add(go.gameObject);

                    if (go.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = fd * projectileSpeed;
                    go.Initialize(this, data.atk);

                    int returnDelay = projectileInterval * (projectileCount - i) + parryWindowDelay + 1000;
                    _ = ReturnParry(go, returnDelay);
                }
                await UniTask.Delay(projectileInterval);
            }

            await UniTask.Delay(300 + parryWindowDelay);
            cts.Token.ThrowIfCancellationRequested();
            await TeleportToRandomPosition();
            cts.Token.ThrowIfCancellationRequested();
            await UniTask.Delay(postAttackDelay);
            cts.Token.ThrowIfCancellationRequested();

            IsActing = false;
            callback?.Invoke(true);
        }
        catch (OperationCanceledException)
        {
            // 취소 시 IsActing 이 true 로 남으면 이후 모든 패턴이 Failure 가 되므로 반드시 해제
            IsActing = false;
            if (animator) animator.SetBool(ActingHash, false);
        }
    }

    async UniTask ReturnParry(ParriableProjectile go, int delay)
    {
        await UniTask.Delay(delay);
        if (go != null && go.gameObject.activeSelf)
            ParryPool.Release(go);
    }

    public async UniTask AttackFirePillar(Action<bool> callback, CancellationTokenSource cts)
    {
        try
        {
            IsActing = true;

            fireSignalReceived = false;
            animator.Play(FireWallHash);
            await UniTask.WaitUntil(() => fireSignalReceived);
            cts.Token.ThrowIfCancellationRequested();

            if (firePillarWarningPrefab)
            {
                for (int i = 0; i < firePillarCount; i++)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    Vector3 spawnPos = new Vector3(
                        UnityEngine.Random.Range(firePillarRangeXMin, firePillarRangeXMax),
                        firePillarGroundY, 0f);

                    var warning = PillarWarningPool.Get();
                    warning.transform.position = spawnPos;
                    activePoolObjects.Add(warning);
                    _ = FirePillarExplode(spawnPos, warning, firePillarWarningDuration);
                }
                await UniTask.Delay(firePillarWarningDuration + 500);
                cts.Token.ThrowIfCancellationRequested();
            }
            else
            {
                await UniTask.Delay(2000);
                cts.Token.ThrowIfCancellationRequested();
            }

            _ = TeleportToRandomPosition();
            await UniTask.Delay(postAttackDelay);
            cts.Token.ThrowIfCancellationRequested();
            IsActing = false;
            callback?.Invoke(true);
        }
        catch (OperationCanceledException)
        {
            // 취소 시 IsActing 이 true 로 남으면 이후 모든 패턴이 Failure 가 되므로 반드시 해제
            IsActing = false;
            if (animator) animator.SetBool(ActingHash, false);
        }
    }

    async UniTask FirePillarExplode(Vector3 pos, GameObject warning, int delay)
    {
        await UniTask.Delay(delay);
        PillarWarningPool.Release(warning);

        if (firePillarExplosionPrefab)
        {
            var explosion = PillarPool.Get();
            explosion.transform.position = pos;
            activePoolObjects.Add(explosion.gameObject);
            explosion.Init(data.atk);
            explosion.Setup();
        }
    }

    public async UniTask AttackBulletCurtain(Action<bool> callback, CancellationTokenSource cts)
    {
        try
        {
            IsActing = true;

            fireSignalReceived = false;
            animator.Play(SpreadFireBallHash);
            await UniTask.WaitUntil(() => fireSignalReceived);
            cts.Token.ThrowIfCancellationRequested();

            if (bulletPrefab)
            {
                float step = 360f / bulletCurtainCount;
                for (int i = 0; i < bulletCurtainCount; i++)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    float angle = i * step * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

                    var bullet = bulletPool.Get();
                    bullet.transform.position = transform.position;
                    activePoolObjects.Add(bullet.gameObject);

                    if (bullet.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = dir * bulletSpeed;
                    bullet.Init(data.atk);

                    _ = ReturnBullet(bullet, bulletLifetime);
                }
            }

            await UniTask.Delay(1000);
            cts.Token.ThrowIfCancellationRequested();
            await TeleportToRandomPosition();
            cts.Token.ThrowIfCancellationRequested();
            await UniTask.Delay(postAttackDelay);
            cts.Token.ThrowIfCancellationRequested();
            IsActing = false;
            callback?.Invoke(true);
        }
        catch (OperationCanceledException)
        {
            // 취소 시 IsActing 이 true 로 남으면 이후 모든 패턴이 Failure 가 되므로 반드시 해제
            IsActing = false;
            if (animator) animator.SetBool(ActingHash, false);
        }
    }

    async UniTask ReturnBullet(BossBullet bullet, int lifetime)
    {
        await UniTask.Delay(lifetime);
        if (bullet != null && bullet.gameObject.activeSelf)
            bulletPool.Release(bullet);
    }

    public async UniTask AttackFloorLaser(Action<bool> callback, CancellationTokenSource cts)
    {
        try
        {
            IsActing = true;

            bool bossOnLeft = transform.position.x <= 0f;
            Transform[] spawnPoints = bossOnLeft ? floorLaserLeftPositions : floorLaserRightPositions;

            animator.SetBool(ActingHash, IsActing);
            animator.Play(LaserHash);

            if (laserPrefab && spawnPoints is { Length: > 0 })
            {
                int[] order = ShuffledOrder(spawnPoints.Length);
                var lasers = new FloorLaser[spawnPoints.Length];

                for (int i = 0; i < order.Length; i++)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    int idx = order[i];
                    if (!spawnPoints[idx]) continue;

                    float halfLen = laserPrefab.transform.localScale.x * 0.4f;
                    float offsetX = bossOnLeft ? halfLen : -halfLen;
                    Vector3 spawnPos = new(spawnPoints[idx].position.x + offsetX,
                                           spawnPoints[idx].position.y, 0f);

                    var go = LaserPool.Get();
                    go.transform.position = spawnPos;
                    activePoolObjects.Add(go.gameObject);
                    go.Init(data.atk);
                    go.StartWarning();
                    lasers[i] = go;

                    await UniTask.Delay(500);
                }

                await UniTask.Delay(laserWarningDuration);
                cts.Token.ThrowIfCancellationRequested();

                foreach (var laser in lasers)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    if (laser == null) continue;
                    laser.Activate(laserActiveDuration);
                    await UniTask.Delay(laserActiveDuration + 200);
                }
            }
            else
            {
                await UniTask.Delay(3000);
                cts.Token.ThrowIfCancellationRequested();
            }

            IsActing = false;
            animator.SetBool(ActingHash, IsActing);
            _ = TeleportToRandomPosition();
            await UniTask.Delay(postAttackDelay);
            cts.Token.ThrowIfCancellationRequested();
            callback?.Invoke(true);
        }
        catch (OperationCanceledException)
        {
            // 취소 시 IsActing 이 true 로 남으면 이후 모든 패턴이 Failure 가 되므로 반드시 해제
            IsActing = false;
            if (animator) animator.SetBool(ActingHash, false);
        }
    }

    public async UniTask AttackGroggyPattern(Action<bool> callback, CancellationTokenSource cts)
    {
        try
        {
            IsActing = true;
            animator.SetBool(ActingHash, IsActing);

            if (groggyCenterPosition != null)
                _ = DoTeleport(groggyCenterPosition.position);

            animator.Play(BigAttackHash);

            groggyPartsTotal = groggyPartSpawnPoints?.Length ?? 0;
            groggyPartsDestroyed = 0;
            spawnedGroggyParts.Clear();

            if (groggyPartPrefab && groggyPartSpawnPoints is { Length: > 0 })
            {
                foreach (var pt in groggyPartSpawnPoints)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    if (!pt) continue;
                    var part = PartPool.Get();
                    part.transform.position = pt.position;
                    activePoolObjects.Add(part.gameObject);
                    spawnedGroggyParts.Add(part);
                    part.Initialize(this);
                }
            }

            float timer = 0f;
            SoundManager.Instance.PlaySFXLoop(ChargeClip);
            try
            {
                while (timer < groggyTimeLimit && groggyPartsDestroyed < groggyPartsTotal && !IsDead)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    timer += Time.deltaTime;
                    await UniTask.Yield(PlayerLoopTiming.LastUpdate);
                }
            }
            finally
            {
                // 취소/사망 시에도 차지 루프 사운드가 무한 재생되지 않도록 보장
                SoundManager.Instance.StopSFXLoop();
            }

            // 그로기 도중 보스가 죽으면 스턴/회복(HealHP 로 되살아나는 문제)/텔레포트 전부 스킵
            if (IsDead) return;

            if (groggyPartsDestroyed >= groggyPartsTotal)
            {
                IsGroggy = true;
                animator.SetBool(StunHash, IsGroggy);
                animator.Play(StunHash);
                await UniTask.Delay(groggyDuration);
                cts.Token.ThrowIfCancellationRequested();
                IsGroggy = false;
                animator.SetBool(StunHash, IsGroggy);
            }
            else
            {
                cts.Token.ThrowIfCancellationRequested();
                foreach (var part in spawnedGroggyParts)
                    if (part != null && part.gameObject.activeSelf) PartPool.Release(part);
                spawnedGroggyParts.Clear();
                HealHP(groggyHPRecoveryAmount);
            }

            IsActing = false;
            animator.SetBool(ActingHash, IsActing);
            await TeleportToRandomPosition();
            cts.Token.ThrowIfCancellationRequested();
            await UniTask.Delay(postAttackDelay);
            cts.Token.ThrowIfCancellationRequested();
            callback?.Invoke(true);
        }
        catch (OperationCanceledException)
        {
            // 취소 시 IsActing 이 true 로 남으면 이후 모든 패턴이 Failure 가 되므로 반드시 해제
            IsActing = false;
            if (animator) animator.SetBool(ActingHash, false);
        }
    }

    public void OnFireSignal() => fireSignalReceived = true;

    public void OnGroggyPartDestroyed() => groggyPartsDestroyed++;

    async UniTask HitFlash()
    {
        if (!spriteRenderer) return;
        Color baseColor = IsPhase2 ? phase2Color : Color.white;
        spriteRenderer.color = Color.black;
        await UniTask.Delay(hitFlashDuration, ignoreTimeScale: true);
        spriteRenderer.color = baseColor;
    }

    async UniTask DeathEffect()
    {
        int token = TimeControl.Claim(0f);
        await UniTask.Delay(deathStopDuration, ignoreTimeScale: true);
        if (!TimeControl.IsOwner(token)) { OnDeath(); return; }

        TimeControl.Set(token, deathSlowScale);
        float elapsed = 0f;
        while (elapsed < deathSlowDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            await UniTask.Yield(PlayerLoopTiming.LastUpdate);
        }

        TimeControl.Release(token);
        OnDeath();
    }

    private int[] ShuffledOrder(int count)
    {
        var order = new int[count];
        for (int i = 0; i < count; i++) order[i] = i;
        for (int i = count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
        return order;
    }

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