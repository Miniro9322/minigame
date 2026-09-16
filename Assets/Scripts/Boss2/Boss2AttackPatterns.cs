using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>보스2의 5가지 공격 패턴(패링 투사체 / 불기둥 / 탄막 / 층 레이저 / 그로기)을 전담한다.</summary>
public class Boss2AttackPatterns
{
    private static readonly int ActingHash        = Animator.StringToHash("Acting");
    private static readonly int LaserHash         = Animator.StringToHash("Laser");
    private static readonly int BigAttackHash     = Animator.StringToHash("BigAttack");
    private static readonly int FireBallHash      = Animator.StringToHash("FireBall");
    private static readonly int SpreadFireBallHash = Animator.StringToHash("SpreadFireBall");
    private static readonly int FireWallHash      = Animator.StringToHash("FireWall");
    private static readonly int StunHash          = Animator.StringToHash("Stun");

    private readonly Boss2Controller controller;
    private readonly Boss2PoolManager pools;
    private readonly Boss2TeleportController teleport;
    private readonly Transform bossTransform;
    private readonly Animator animator;
    private readonly Transform playerTransform;
    private readonly BossData data;
    private readonly int postAttackDelay;
    private readonly int parryWindowDelay;

    private readonly ParryProjectileConfig parryConfig;
    private readonly FirePillarConfig firePillarConfig;
    private readonly BulletCurtainConfig bulletCurtainConfig;
    private readonly FloorLaserConfig floorLaserConfig;
    private readonly GroggyPatternConfig groggyConfig;

    private bool fireSignalReceived = false;

    private int groggyPartsTotal     = 0;
    private int groggyPartsDestroyed = 0;
    private readonly List<GroggyPart> spawnedGroggyParts = new();

    public Boss2AttackPatterns(
        Boss2Controller controller,
        Boss2PoolManager pools,
        Boss2TeleportController teleport,
        Transform bossTransform,
        Animator animator,
        Transform playerTransform,
        BossData data,
        int postAttackDelay,
        int parryWindowDelay,
        ParryProjectileConfig parryConfig,
        FirePillarConfig firePillarConfig,
        BulletCurtainConfig bulletCurtainConfig,
        FloorLaserConfig floorLaserConfig,
        GroggyPatternConfig groggyConfig)
    {
        this.controller       = controller;
        this.pools            = pools;
        this.teleport         = teleport;
        this.bossTransform    = bossTransform;
        this.animator         = animator;
        this.playerTransform  = playerTransform;
        this.data             = data;
        this.postAttackDelay  = postAttackDelay;
        this.parryWindowDelay = parryWindowDelay;
        this.parryConfig         = parryConfig;
        this.firePillarConfig    = firePillarConfig;
        this.bulletCurtainConfig = bulletCurtainConfig;
        this.floorLaserConfig    = floorLaserConfig;
        this.groggyConfig        = groggyConfig;
    }

    public void OnFireSignal() => fireSignalReceived = true;

    public void OnGroggyPartDestroyed() => groggyPartsDestroyed++;

    /// <summary>보스 사망 시 호출 — 남은 그로기 파츠를 즉시 파괴한다.</summary>
    public void ClearSpawnedGroggyParts()
    {
        foreach (var part in spawnedGroggyParts)
            if (part != null) UnityEngine.Object.Destroy(part.gameObject);
        spawnedGroggyParts.Clear();
    }

    public async UniTask AttackParriableProjectile(Action<bool> callback, CancellationTokenSource cts)
    {
        try
        {
            controller.IsActing = true;

            fireSignalReceived = false;

            animator.Play(FireBallHash);

            await UniTask.WaitUntil(() => fireSignalReceived);

            cts.Token.ThrowIfCancellationRequested();

            for (int i = 0; i < parryConfig.Count; i++)
            {
                cts.Token.ThrowIfCancellationRequested();

                if (parryConfig.Prefab && playerTransform)
                {
                    Vector3 dir = (playerTransform.position - bossTransform.position).normalized;
                    float spread = UnityEngine.Random.Range(-10f, 10f) * Mathf.Deg2Rad;
                    Vector2 fd = new Vector2(
                        dir.x * Mathf.Cos(spread) - dir.y * Mathf.Sin(spread),
                        dir.x * Mathf.Sin(spread) + dir.y * Mathf.Cos(spread)).normalized;

                    var go = pools.ParryPool.Get();
                    go.transform.position = bossTransform.position;
                    pools.ActivePoolObjects.Add(go.gameObject);

                    if (go.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = fd * parryConfig.Speed;
                    go.Initialize(controller, data.atk);

                    int returnDelay = parryConfig.Interval * (parryConfig.Count - i) + parryWindowDelay + 1000;
                    _ = ReturnParry(go, returnDelay);
                }
                await UniTask.Delay(parryConfig.Interval);
            }

            await UniTask.Delay(300 + parryWindowDelay);
            cts.Token.ThrowIfCancellationRequested();
            await teleport.TeleportToRandomPosition();
            cts.Token.ThrowIfCancellationRequested();
            await UniTask.Delay(postAttackDelay);
            cts.Token.ThrowIfCancellationRequested();

            controller.IsActing = false;
            callback?.Invoke(true);
        }
        catch (OperationCanceledException)
        {
            // 취소 시 IsActing 이 true 로 남으면 이후 모든 패턴이 Failure 가 되므로 반드시 해제
            controller.IsActing = false;
            if (animator) animator.SetBool(ActingHash, false);
        }
    }

    private async UniTask ReturnParry(ParriableProjectile go, int delay)
    {
        await UniTask.Delay(delay);
        if (go != null && go.gameObject.activeSelf)
            pools.ParryPool.Release(go);
    }

    public async UniTask AttackFirePillar(Action<bool> callback, CancellationTokenSource cts)
    {
        try
        {
            controller.IsActing = true;

            fireSignalReceived = false;
            animator.Play(FireWallHash);
            await UniTask.WaitUntil(() => fireSignalReceived);
            cts.Token.ThrowIfCancellationRequested();

            if (firePillarConfig.WarningPrefab)
            {
                for (int i = 0; i < firePillarConfig.Count; i++)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    Vector3 spawnPos = new Vector3(
                        UnityEngine.Random.Range(firePillarConfig.RangeXMin, firePillarConfig.RangeXMax),
                        firePillarConfig.GroundY, 0f);

                    var warning = pools.PillarWarningPool.Get();
                    warning.transform.position = spawnPos;
                    pools.ActivePoolObjects.Add(warning);
                    _ = FirePillarExplode(spawnPos, warning, firePillarConfig.WarningDuration);
                }
                await UniTask.Delay(firePillarConfig.WarningDuration + 500);
                cts.Token.ThrowIfCancellationRequested();
            }
            else
            {
                await UniTask.Delay(2000);
                cts.Token.ThrowIfCancellationRequested();
            }

            _ = teleport.TeleportToRandomPosition();
            await UniTask.Delay(postAttackDelay);
            cts.Token.ThrowIfCancellationRequested();
            controller.IsActing = false;
            callback?.Invoke(true);
        }
        catch (OperationCanceledException)
        {
            // 취소 시 IsActing 이 true 로 남으면 이후 모든 패턴이 Failure 가 되므로 반드시 해제
            controller.IsActing = false;
            if (animator) animator.SetBool(ActingHash, false);
        }
    }

    private async UniTask FirePillarExplode(Vector3 pos, GameObject warning, int delay)
    {
        await UniTask.Delay(delay);
        pools.PillarWarningPool.Release(warning);

        if (firePillarConfig.ExplosionPrefab)
        {
            var explosion = pools.PillarPool.Get();
            explosion.transform.position = pos;
            pools.ActivePoolObjects.Add(explosion.gameObject);
            explosion.Init(data.atk);
            explosion.Setup();
        }
    }

    public async UniTask AttackBulletCurtain(Action<bool> callback, CancellationTokenSource cts)
    {
        try
        {
            controller.IsActing = true;

            fireSignalReceived = false;
            animator.Play(SpreadFireBallHash);
            await UniTask.WaitUntil(() => fireSignalReceived);
            cts.Token.ThrowIfCancellationRequested();

            if (bulletCurtainConfig.Prefab)
            {
                float step = 360f / bulletCurtainConfig.Count;
                for (int i = 0; i < bulletCurtainConfig.Count; i++)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    float angle = i * step * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

                    var bullet = pools.BulletPool.Get();
                    bullet.transform.position = bossTransform.position;
                    pools.ActivePoolObjects.Add(bullet.gameObject);

                    if (bullet.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = dir * bulletCurtainConfig.Speed;
                    bullet.Init(data.atk);

                    _ = ReturnBullet(bullet, bulletCurtainConfig.Lifetime);
                }
            }

            await UniTask.Delay(1000);
            cts.Token.ThrowIfCancellationRequested();
            await teleport.TeleportToRandomPosition();
            cts.Token.ThrowIfCancellationRequested();
            await UniTask.Delay(postAttackDelay);
            cts.Token.ThrowIfCancellationRequested();
            controller.IsActing = false;
            callback?.Invoke(true);
        }
        catch (OperationCanceledException)
        {
            // 취소 시 IsActing 이 true 로 남으면 이후 모든 패턴이 Failure 가 되므로 반드시 해제
            controller.IsActing = false;
            if (animator) animator.SetBool(ActingHash, false);
        }
    }

    private async UniTask ReturnBullet(BossBullet bullet, int lifetime)
    {
        await UniTask.Delay(lifetime);
        if (bullet != null && bullet.gameObject.activeSelf)
            pools.BulletPool.Release(bullet);
    }

    public async UniTask AttackFloorLaser(Action<bool> callback, CancellationTokenSource cts)
    {
        try
        {
            controller.IsActing = true;

            bool bossOnLeft = bossTransform.position.x <= 0f;
            Transform[] spawnPoints = bossOnLeft ? floorLaserConfig.LeftPositions : floorLaserConfig.RightPositions;

            animator.SetBool(ActingHash, controller.IsActing);
            animator.Play(LaserHash);

            if (floorLaserConfig.Prefab && spawnPoints is { Length: > 0 })
            {
                int[] order = ShuffledOrder(spawnPoints.Length);
                var lasers = new FloorLaser[spawnPoints.Length];

                for (int i = 0; i < order.Length; i++)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    int idx = order[i];
                    if (!spawnPoints[idx]) continue;

                    float halfLen = floorLaserConfig.Prefab.transform.localScale.x * 0.4f;
                    float offsetX = bossOnLeft ? halfLen : -halfLen;
                    Vector3 spawnPos = new(spawnPoints[idx].position.x + offsetX,
                                           spawnPoints[idx].position.y, 0f);

                    var go = pools.LaserPool.Get();
                    go.transform.position = spawnPos;
                    pools.ActivePoolObjects.Add(go.gameObject);
                    go.Init(data.atk);
                    go.StartWarning();
                    lasers[i] = go;

                    await UniTask.Delay(500);
                }

                await UniTask.Delay(floorLaserConfig.WarningDuration);
                cts.Token.ThrowIfCancellationRequested();

                foreach (var laser in lasers)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    if (laser == null) continue;
                    laser.Activate(floorLaserConfig.ActiveDuration);
                    await UniTask.Delay(floorLaserConfig.ActiveDuration + 200);
                }
            }
            else
            {
                await UniTask.Delay(3000);
                cts.Token.ThrowIfCancellationRequested();
            }

            controller.IsActing = false;
            animator.SetBool(ActingHash, controller.IsActing);
            _ = teleport.TeleportToRandomPosition();
            await UniTask.Delay(postAttackDelay);
            cts.Token.ThrowIfCancellationRequested();
            callback?.Invoke(true);
        }
        catch (OperationCanceledException)
        {
            // 취소 시 IsActing 이 true 로 남으면 이후 모든 패턴이 Failure 가 되므로 반드시 해제
            controller.IsActing = false;
            if (animator) animator.SetBool(ActingHash, false);
        }
    }

    public async UniTask AttackGroggyPattern(Action<bool> callback, CancellationTokenSource cts)
    {
        try
        {
            controller.IsActing = true;
            animator.SetBool(ActingHash, controller.IsActing);

            if (groggyConfig.CenterPosition != null)
                _ = teleport.DoTeleport(groggyConfig.CenterPosition.position);

            animator.Play(BigAttackHash);

            groggyPartsTotal = groggyConfig.SpawnPoints?.Length ?? 0;
            groggyPartsDestroyed = 0;
            spawnedGroggyParts.Clear();

            if (groggyConfig.PartPrefab && groggyConfig.SpawnPoints is { Length: > 0 })
            {
                foreach (var pt in groggyConfig.SpawnPoints)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    if (!pt) continue;
                    var part = pools.PartPool.Get();
                    part.transform.position = pt.position;
                    pools.ActivePoolObjects.Add(part.gameObject);
                    spawnedGroggyParts.Add(part);
                    part.Initialize(controller);
                }
            }

            float timer = 0f;
            SoundManager.Instance.PlaySFXLoop(groggyConfig.ChargeClip);
            try
            {
                while (timer < groggyConfig.TimeLimit && groggyPartsDestroyed < groggyPartsTotal && !controller.IsDead)
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
            if (controller.IsDead) return;

            if (groggyPartsDestroyed >= groggyPartsTotal)
            {
                controller.IsGroggy = true;
                animator.SetBool(StunHash, controller.IsGroggy);
                animator.Play(StunHash);
                await UniTask.Delay(groggyConfig.Duration);
                cts.Token.ThrowIfCancellationRequested();
                controller.IsGroggy = false;
                animator.SetBool(StunHash, controller.IsGroggy);
            }
            else
            {
                cts.Token.ThrowIfCancellationRequested();
                foreach (var part in spawnedGroggyParts)
                    if (part != null && part.gameObject.activeSelf) pools.PartPool.Release(part);
                spawnedGroggyParts.Clear();
                controller.HealHP(groggyConfig.HpRecoveryAmount);
            }

            controller.IsActing = false;
            animator.SetBool(ActingHash, controller.IsActing);
            await teleport.TeleportToRandomPosition();
            cts.Token.ThrowIfCancellationRequested();
            await UniTask.Delay(postAttackDelay);
            cts.Token.ThrowIfCancellationRequested();
            callback?.Invoke(true);
        }
        catch (OperationCanceledException)
        {
            // 취소 시 IsActing 이 true 로 남으면 이후 모든 패턴이 Failure 가 되므로 반드시 해제
            controller.IsActing = false;
            if (animator) animator.SetBool(ActingHash, false);
        }
    }

    private static int[] ShuffledOrder(int count)
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
}
