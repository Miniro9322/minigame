using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Scriptable Objects/PlayerData")]
public class PlayerData : ScriptableObject
{
    [Header("── 스탯 ──")]
    public int Atk;
    public int MaxHp;
    public int MoveSpeed;

    [Header("── 점프 ──")]
    [Tooltip("플레이어 점프 힘")]
    public float JumpPower = 10f;

    public int MaxJumpCount = 1;

    [Tooltip("착지 판정 원의 반지름")]
    public float GroundCheckRadius = 0.15f;

    [Tooltip("지면에서 벗어난 뒤에도 점프를 허용하는 시간 (코요테 타임)")]
    public float CoyoteTime = 0.1f;

    [Tooltip("착지 전에 누른 점프 입력을 기억해 두는 시간 (점프 버퍼)")]
    public float JumpBufferTime = 0.1f;

    [Tooltip("상승 중 점프 버튼을 떼면 곱해지는 중력 배수 (가변 점프)")]
    public float LowJumpMultiplier = 2f;

    [Tooltip("하강 구간에 곱해지는 중력 배수 (점프 궤적에 무게감 부여)")]
    public float FallMultiplier = 2.5f;

    [Header("── 낙하 판정 ──")]
    [Tooltip("착지 직후 Grounded 가 깜빡이는 구간을 무시할 프레임 수. " +
             "이 프레임을 넘겨야 FallState 로 전이한다 (공중 걷기 방지)")]
    public int FallStateGraceFrames = 2;

    [Tooltip("이 속도보다 빠르게 하강해야 낙하로 간주 (부호는 코드에서 음수로 적용)")]
    public float FallVelocityThreshold = 0.01f;

    [Header("── 공격 ──")]
    [Tooltip("최대 콤보 단수")]
    public int MaxAttackCount = 2;

    [Header("── 회피(대시) ──")]
    [Tooltip("대시 이동 거리")]
    public float DodgeAmount = 5f;

    [Tooltip("대시 지속 시간")]
    public float DodgeDuration = 0.5f;

    [Tooltip("대시 재사용 대기시간")]
    public float DodgeCooldown = 1f;

    [Tooltip("대시 시작 후 이 시간 안에 공격을 누르면 '회피 공격'으로 캔슬 연결된다")]
    public float DodgeAttackWindow = 0.2f;

    [Header("── 패링 ──")]
    [Tooltip("패링 판정이 유지되는 시간")]
    public float ParryInterval = 0.3f;

    [Header("── 낙하 공격 ──")]
    [Tooltip("낙하 공격 하강 속도")]
    public float PlungeSpeed = 20f;

    [Tooltip("공중에서 아래 입력을 낙하 공격 선입력으로 인식할 스틱 임계값")]
    public float DownInputThreshold = 0.5f;

    [Header("── 피격 ──")]
    [Tooltip("피격 시 넉백 수평 힘")]
    public float KnockbackForceX = 5f;

    [Tooltip("피격 시 넉백 수직 힘")]
    public float KnockbackForceY = 4f;

    [Tooltip("피격 후 무적/점멸이 유지되는 총 시간")]
    public float BlinkInterval = 1f;

    [Tooltip("점멸 1회 주기")]
    public float BlinkDurationInterval = 0.16f;

    [Header("── 사망 연출 ──")]
    [Tooltip("사망 직후 완전 정지(timeScale 0) 시간. unscaled 기준")]
    public float DeathStopDuration = 0.3f;

    [Tooltip("정지 후 사망 애니메이션을 재생할 슬로모션 배속")]
    public float DeathSlowScale = 0.3f;

    [Header("── 크라우칭 ──")]
    public float StandColliderHeight = 1.6f;
    public float StandColliderOffsetY = 0f;
    public float CrouchColliderHeight = 0.8f;
    public float CrouchColliderOffsetY = -0.4f;

    [Tooltip("앉은 상태의 이동 속도 배수")]
    public float CrouchSpeedMultiplier = 0.4f;
}
