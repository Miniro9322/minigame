using UnityEngine;

/// <summary>Boss2AttackPatterns 생성자에 넘기는 패턴별 튜닝값 묶음. Boss2Controller의 인스펙터 필드 값을 그대로 옮겨 담는다.</summary>
public readonly struct ParryProjectileConfig
{
    public readonly GameObject Prefab;
    public readonly int   Count;
    public readonly float Speed;
    public readonly int   Interval;

    public ParryProjectileConfig(GameObject prefab, int count, float speed, int interval)
    {
        Prefab = prefab; Count = count; Speed = speed; Interval = interval;
    }
}

public readonly struct FirePillarConfig
{
    public readonly GameObject WarningPrefab;
    public readonly GameObject ExplosionPrefab;
    public readonly int   Count;
    public readonly int   WarningDuration;
    public readonly float GroundY;
    public readonly float RangeXMin;
    public readonly float RangeXMax;

    public FirePillarConfig(GameObject warningPrefab, GameObject explosionPrefab, int count,
        int warningDuration, float groundY, float rangeXMin, float rangeXMax)
    {
        WarningPrefab = warningPrefab; ExplosionPrefab = explosionPrefab; Count = count;
        WarningDuration = warningDuration; GroundY = groundY; RangeXMin = rangeXMin; RangeXMax = rangeXMax;
    }
}

public readonly struct BulletCurtainConfig
{
    public readonly GameObject Prefab;
    public readonly int   Count;
    public readonly float Speed;
    public readonly int   Lifetime;

    public BulletCurtainConfig(GameObject prefab, int count, float speed, int lifetime)
    {
        Prefab = prefab; Count = count; Speed = speed; Lifetime = lifetime;
    }
}

public readonly struct FloorLaserConfig
{
    public readonly GameObject Prefab;
    public readonly Transform[] LeftPositions;
    public readonly Transform[] RightPositions;
    public readonly int WarningDuration;
    public readonly int ActiveDuration;

    public FloorLaserConfig(GameObject prefab, Transform[] leftPositions, Transform[] rightPositions,
        int warningDuration, int activeDuration)
    {
        Prefab = prefab; LeftPositions = leftPositions; RightPositions = rightPositions;
        WarningDuration = warningDuration; ActiveDuration = activeDuration;
    }
}

public readonly struct GroggyPatternConfig
{
    public readonly GameObject PartPrefab;
    public readonly Transform[] SpawnPoints;
    public readonly Transform CenterPosition;
    public readonly float TimeLimit;
    public readonly int   Duration;
    public readonly float HpRecoveryAmount;
    public readonly AudioClip ChargeClip;

    public GroggyPatternConfig(GameObject partPrefab, Transform[] spawnPoints, Transform centerPosition,
        float timeLimit, int duration, float hpRecoveryAmount, AudioClip chargeClip)
    {
        PartPrefab = partPrefab; SpawnPoints = spawnPoints; CenterPosition = centerPosition;
        TimeLimit = timeLimit; Duration = duration; HpRecoveryAmount = hpRecoveryAmount; ChargeClip = chargeClip;
    }
}
