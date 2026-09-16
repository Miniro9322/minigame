using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>Boss2Controller가 사용하는 6종 오브젝트 풀과, 현재 스폰되어 있는 오브젝트 추적을 전담한다.</summary>
public class Boss2PoolManager
{
    public List<GameObject> ActivePoolObjects { get; } = new();

    public IObjectPool<BossBullet>          BulletPool        { get; }
    public IObjectPool<FirePillar>          PillarPool        { get; }
    public IObjectPool<GameObject>          PillarWarningPool { get; }
    public IObjectPool<FloorLaser>          LaserPool         { get; }
    public IObjectPool<GroggyPart>          PartPool          { get; }
    public IObjectPool<ParriableProjectile> ParryPool         { get; }

    public Boss2PoolManager(
        GameObject bulletPrefab,
        GameObject firePillarExplosionPrefab,
        GameObject firePillarWarningPrefab,
        GameObject laserPrefab,
        GameObject groggyPartPrefab,
        GameObject parriableProjectilePrefab)
    {
        BulletPool        = MakeComponentPool<BossBullet>         (bulletPrefab);
        PillarPool        = MakeComponentPool<FirePillar>          (firePillarExplosionPrefab, (o, p) => o.ObjectPool = p);
        PillarWarningPool = MakeGameObjectPool                     (firePillarWarningPrefab);
        LaserPool         = MakeComponentPool<FloorLaser>          (laserPrefab,               (o, p) => o.ObjectPool = p);
        PartPool          = MakeComponentPool<GroggyPart>          (groggyPartPrefab,          (o, p) => o.ObjectPool = p);
        ParryPool         = MakeComponentPool<ParriableProjectile> (parriableProjectilePrefab, (o, p) => o.ObjectPool = p);
    }

    /// <summary>프리팹에서 컴포넌트 T 를 뽑아내는 오브젝트 풀 생성. onCreate 로 풀 역참조 등을 주입한다.</summary>
    private ObjectPool<T> MakeComponentPool<T>(GameObject prefab, Action<T, IObjectPool<T>> onCreate = null)
        where T : Component
    {
        ObjectPool<T> pool = null;
        pool = new ObjectPool<T>(
            createFunc:      () => { var o = UnityEngine.Object.Instantiate(prefab).GetComponent<T>(); onCreate?.Invoke(o, pool); return o; },
            actionOnGet:     p => p.gameObject.SetActive(true),
            actionOnRelease: p => { p.gameObject.SetActive(false); ActivePoolObjects.Remove(p.gameObject); },
            actionOnDestroy: p => { if (p) UnityEngine.Object.Destroy(p.gameObject); });
        return pool;
    }

    private ObjectPool<GameObject> MakeGameObjectPool(GameObject prefab)
        => new(
            createFunc:      () => UnityEngine.Object.Instantiate(prefab),
            actionOnGet:     p => p.SetActive(true),
            actionOnRelease: p => { p.SetActive(false); ActivePoolObjects.Remove(p); },
            actionOnDestroy: p => { if (p) UnityEngine.Object.Destroy(p); });

    /// <summary>현재 스폰된 모든 풀 오브젝트를 즉시 파괴하고 풀 자체도 비운다 (보스 사망 시 호출).</summary>
    public void DestroyAllActive()
    {
        foreach (var obj in ActivePoolObjects)
            if (obj != null) UnityEngine.Object.Destroy(obj);
        ActivePoolObjects.Clear();

        BulletPool?.Clear();
        PillarPool?.Clear();
        PillarWarningPool?.Clear();
        LaserPool?.Clear();
        PartPool?.Clear();
        ParryPool?.Clear();
    }
}
