using UnityEngine;

/// <summary>플레이어의 지면 판정, 점프(코요테 타임/점프 버퍼), 좌우 이동, 웅크리기 콜라이더를 전담한다.</summary>
public class PlayerMovementController
{
    private static readonly int MoveBool = Animator.StringToHash("Move");

    private readonly Player player;
    private readonly Transform groundCheck;
    private readonly LayerMask groundLayer;
    private readonly BoxCollider2D boxcollider;

    private Vector2 move;
    private int jumpCount = 0;
    private float jumpBufferCounter = 0f;
    private float coyoteCounter = 0f;
    private int notGroundedFrames = 0;

    /// <summary>
    /// 낙하 상태로 전이해야 하는지 여부. 착지 직후 grace 프레임을 넘긴 뒤 하강 중일 때 true.
    /// 지상에 설 수 있는 상태(Idle/Crouch/Parry)의 FixedUpdate 에서 이 값으로 FallState 로 전이한다.
    /// </summary>
    public bool ShouldFall =>
        !player.Grounded
        && player.Rb.linearVelocity.y < -player.Data.FallVelocityThreshold
        && notGroundedFrames > player.Data.FallStateGraceFrames;

    public PlayerMovementController(Player player, Transform groundCheck, LayerMask groundLayer, BoxCollider2D boxcollider)
    {
        this.player      = player;
        this.groundCheck = groundCheck;
        this.groundLayer = groundLayer;
        this.boxcollider = boxcollider;
    }

    public void RequestJumpBuffer() => jumpBufferCounter = player.Data.JumpBufferTime;

    /// <summary>OnMove 입력 콜백에서 호출 — 공중에서 아래로 꺾는 입력을 다운 커맨드로 큐잉한다.</summary>
    public void HandleMoveInput(Vector2 newMove)
    {
        float downThreshold = -player.Data.DownInputThreshold;

        if (!player.Grounded && newMove.y < downThreshold && move.y >= downThreshold)
        {
            player.CommandQueue.Clear();
            player.CommandQueue.Enqueue(PlayerCommand.Down);
        }

        move = newMove;
    }

    public void FixedUpdateTick()
    {
        // 상태의 FixedUpdate 에서 player.ShouldFall 을 읽으므로 그 전에 갱신해 둔다
        player.Grounded = Physics2D.OverlapCircle(groundCheck.position, player.Data.GroundCheckRadius, groundLayer);

        if (player.Grounded) notGroundedFrames = 0;
        else                  notGroundedFrames++;

        player.Fsm.FixedUpdate();

        if (player.Fsm.CurrentState == player.DodgeState || player.Fsm.CurrentState == player.DeathState)
            return;

        Move(move);

        if (player.Grounded)
        {
            jumpCount = 0;
            coyoteCounter = player.Data.CoyoteTime;

            if (player.CommandQueue.Count > 0 && player.CommandQueue.Peek() == PlayerCommand.Down)
                player.CommandQueue.Dequeue();
        }
        else
        {
            if (coyoteCounter > 0f)
            {
                coyoteCounter -= Time.fixedDeltaTime;
                if (coyoteCounter <= 0f && jumpCount == 0)
                    jumpCount = 1;
            }
        }

        jumpBufferCounter -= Time.fixedDeltaTime;

        bool isAttacking = player.Fsm.CurrentState == player.AttackState;
        bool isHit       = player.Fsm.CurrentState == player.HitState;

        if (jumpBufferCounter > 0f && (coyoteCounter > 0f || jumpCount < player.Data.MaxJumpCount))
        {
            player.Rb.linearVelocity = new Vector2(player.Rb.linearVelocity.x, player.Data.JumpPower);

            if (!isAttacking && !isHit && player.Fsm.CurrentState != player.JumpState)
                player.Fsm.ChangeState(player.JumpState);

            jumpCount++;
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        if (isAttacking || isHit)
        {
            if (!player.JumpHeld && player.Rb.linearVelocity.y > 0f)
            {
                player.Rb.linearVelocity += (player.Data.LowJumpMultiplier - 1f)
                    * Physics2D.gravity.y * Time.fixedDeltaTime * Vector2.up;
            }

            if (player.Rb.linearVelocity.y < 0f)
            {
                player.Rb.linearVelocity += (player.Data.FallMultiplier - 1f)
                    * Physics2D.gravity.y * Time.fixedDeltaTime * Vector2.up;
            }
        }

        // 낙하 전이는 각 지상 상태(Idle/Crouch/Parry)의 FixedUpdate 에서 player.ShouldFall 로 처리한다.
    }

    public void Move(Vector2 move)
    {
        if (move.x < 0f)
        {
            player.transform.localScale = new Vector3(-1f, 1f, 1f);
            player.Animator.SetBool(MoveBool, true);
        }
        else if (move.x > 0f)
        {
            player.transform.localScale = new Vector3(1f, 1f, 1f);
            player.Animator.SetBool(MoveBool, true);
        }
        else
        {
            player.Animator.SetBool(MoveBool, false);
            player.Rb.linearVelocity = new Vector2(0f, player.Rb.linearVelocity.y);
        }

        float speedMultiplier = player.Fsm.CurrentState == player.CrouchState ? player.Data.CrouchSpeedMultiplier : 1f;
        player.transform.position += player.Data.MoveSpeed * speedMultiplier * Time.fixedDeltaTime * new Vector3(move.x, 0f);
    }

    public void SetColliderCrouch(bool crouch)
    {
        if (boxcollider == null) return;
        if (crouch)
        {
            boxcollider.size   = new Vector2(boxcollider.size.x, player.Data.CrouchColliderHeight);
            boxcollider.offset = new Vector2(boxcollider.offset.x, player.Data.CrouchColliderOffsetY);
        }
        else
        {
            boxcollider.size   = new Vector2(boxcollider.size.x, player.Data.StandColliderHeight);
            boxcollider.offset = new Vector2(boxcollider.offset.x, player.Data.StandColliderOffsetY);
        }
    }
}
