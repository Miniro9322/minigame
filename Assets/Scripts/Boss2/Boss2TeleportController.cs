using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>보스2의 층별 텔레포트 좌표 계산과 이동 연출을 전담한다.</summary>
public class Boss2TeleportController
{
    private const int FloorCount = 4;

    private readonly Transform transform;
    private readonly SpriteRenderer spriteRenderer;
    private readonly int teleportDuration;
    private readonly Vector3[,] teleportPositions;

    public int CurrentFloor { get; private set; }
    public int CurrentSide  { get; private set; }

    public Boss2TeleportController(
        Transform transform,
        SpriteRenderer spriteRenderer,
        int teleportDuration,
        Transform[] floorLeftPositions,
        Transform[] floorCenterPositions,
        Transform[] floorRightPositions,
        int initialFloor,
        int initialSide)
    {
        this.transform        = transform;
        this.spriteRenderer   = spriteRenderer;
        this.teleportDuration = teleportDuration;
        CurrentFloor = initialFloor;
        CurrentSide  = initialSide;

        teleportPositions = new Vector3[FloorCount, 3];
        for (int i = 0; i < FloorCount; i++)
        {
            if (i < floorLeftPositions.Length   && floorLeftPositions[i])
                teleportPositions[i, 0] = floorLeftPositions[i].position;
            if (i < floorCenterPositions.Length && floorCenterPositions[i])
                teleportPositions[i, 1] = floorCenterPositions[i].position;
            if (i < floorRightPositions.Length  && floorRightPositions[i])
                teleportPositions[i, 2] = floorRightPositions[i].position;
        }
    }

    public Vector3 GetPosition(int floor, int side) => teleportPositions[floor, side];

    public void SetCurrentPosition(int floor, int side)
    {
        CurrentFloor = floor;
        CurrentSide  = side;
    }

    public async UniTask DoTeleport(Vector3 target)
    {
        if (spriteRenderer) spriteRenderer.enabled = false;
        await UniTask.Delay(teleportDuration / 2);
        transform.position = target;
        await UniTask.Delay(teleportDuration / 2);
        if (spriteRenderer) spriteRenderer.enabled = true;
    }

    public async UniTask TeleportToRandomPosition()
    {
        var candidates = new List<(int floor, int side)>();
        for (int f = 0; f < FloorCount; f++)
            for (int s = 0; s < 3; s++)
                if (teleportPositions[f, s] != Vector3.zero)
                    candidates.Add((f, s));

        candidates.RemoveAll(c => c.floor == CurrentFloor && c.side == CurrentSide);
        if (candidates.Count == 0) return;

        var pick = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        await DoTeleport(teleportPositions[pick.floor, pick.side]);
        CurrentFloor = pick.floor;
        CurrentSide  = pick.side;
    }
}
