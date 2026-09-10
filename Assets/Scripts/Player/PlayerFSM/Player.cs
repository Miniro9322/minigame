using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour, IDamageable
{
    private static readonly int MoveBool = Animator.StringToHash("Move");

    [SerializeField] private Transform groundCheck;
    [SerializeField] private AttackZone attackZone;
    [SerializeField] private LayerMask groundLayer;

    public FSM Fsm { get; private set; }
    public DashAfterImage AfterImage { get; private set; }
    public Animator Animator { get; private set; }
    public Queue<PlayerCommand> CommandQueue { get; private set; } = new();
    public IState AttackState { get; private set; }
    public IState DeathState { get; private set; }
    public IState DodgeState { get; private set; }
    public IState FallState { get; private set; }
    public IState IdleState { get; private set; }
    public IState JumpState { get; private set; }
    public IState ParryState { get; private set; }
    public IState HitState { get; private set; }
    public IState PlungeState { get; private set; }
    public IState CrouchState { get; private set; }
    public float OriginalGravityScale { get; private set; }
    public bool JumpHeld { get; private set; } = false;
    public bool Grounded { get; private set; }
    public bool IsAttackEnd { get; private set; } = false;
    public Vector2 KnockbackDir { get; private set; }

    /// <summary>
    /// 낙하 상태로 전이해야 하는지 여부. 착지 직후 grace 프레임을 넘긴 뒤 하강 중일 때 true.
    /// 지상에 설 수 있는 상태(Idle/Crouch/Parry)의 FixedUpdate 에서 이 값으로 FallState 로 전이한다.
    /// </summary>
    public bool ShouldFall =>
        !Grounded
        && Rb.linearVelocity.y < -0.01f
        && notGroundedFrames > FallStateGraceFrames;

    public UnityEvent SuccessParry;
    public UnityEvent OnGameOver;
    public UnityEvent OnHit;
    public UnityEvent<int, int> OnHpChange;
    [SerializeField] private ParticleSystem effect1;
    [SerializeField] private ParticleSystem effect2;
    [SerializeField] private ParticleSystem effect3;
    [SerializeField] private ParticleSystem dodgeAttackEffect;
    [SerializeField] private ParticleSystem parryEffect;
    [SerializeField] private ParticleSystem plungeEffect;
    [SerializeField] private AudioClip LandSound;
    public UnityEvent ParryStart;
    public UnityEvent GamePause;
    public PlayerData Data;
    public AttackZone downAttackZone;
    public Rigidbody2D Rb => rb;
    public SpriteRenderer Sr => sr;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private BoxCollider2D boxcollider;
    private InputAction Jump;
    private InputAction Attack;
    private InputAction Dodge;
    private InputAction Parry;
    private InputAction Pause;
    private InputAction Down;

    // notGroundedFrames 가 이 값을 넘으면 낙하 상태로 전이 (착지 직후 1~2 프레임 튐 방지)
    private const int FallStateGraceFrames = 2;

    private Vector2 move;
    private int currHp;
    private int jumpCount = 0;
    private bool invincible = false;
    private bool parrying = false;
    private float jumpBufferCounter = 0f;
    private float coyoteCounter = 0f;
    private float dodgeCool = 0f;
    private int notGroundedFrames = 0;
    

    private void Awake()
    {
        Animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        boxcollider = GetComponent<BoxCollider2D>();
        attackZone.gameObject.SetActive(false);
        OriginalGravityScale = rb.gravityScale;
        AfterImage = GetComponent<DashAfterImage>();
        sr = GetComponent<SpriteRenderer>();
        downAttackZone.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        Jump = InputSystem.actions.FindAction("Jump");
        Attack = InputSystem.actions.FindAction("Attack");
        Dodge = InputSystem.actions.FindAction("Dodge");
        Parry = InputSystem.actions.FindAction("Parry");
        Pause = InputSystem.actions.FindAction("Pause");

        AttackState = new AttackState(this);
        DeathState = new DeathState(this);
        DodgeState = new DodgeState(this);
        FallState = new FallState(this);
        IdleState = new IdleState(this);
        JumpState = new JumpState(this);
        ParryState = new ParryState(this);
        HitState = new HitState(this);
        PlungeState = new PlungeState(this);
        CrouchState = new CrouchState(this);
        Fsm = new FSM();

        Down = InputSystem.actions.FindAction("Down");

        Jump.performed += OnJump;
        Jump.canceled += OnJump;
        Attack.performed += OnAttack;
        Dodge.performed += OnDodge;
        Parry.performed += OnParry;
        Pause.performed += OnPause;
        Down.performed += OnDown;
        Down.canceled += OnDown;
        currHp = Data.MaxHp;
        dodgeCool = Data.DodgeCooldown;
        OnHpChange?.Invoke(currHp, Data.MaxHp);
    }

    private void OnDisable()
    {
        Jump.performed -= OnJump;
        Jump.canceled -= OnJump;
        Attack.performed -= OnAttack;
        Dodge.performed -= OnDodge;
        Parry.performed -= OnParry;
        Down.performed -= OnDown;
        Down.canceled -= OnDown;
        Pause.performed -= OnPause;
    }

    private void Start() => Fsm.ChangeState(IdleState);

    public void OnMove(InputAction.CallbackContext context)
    {
        var newMove = context.ReadValue<Vector2>();

        if (!Grounded && newMove.y < -0.5f && move.y >= -0.5f)
        {
            CommandQueue.Clear();
            CommandQueue.Enqueue(PlayerCommand.Down);
        }

        move = newMove;
    }

    private void Update()
    {
        if (Fsm.CurrentState == DeathState)
            return;

        if(dodgeCool < Data.DodgeCooldown)
        {
            dodgeCool += Time.deltaTime;
        }

        Fsm.Update();
    }

    private void FixedUpdate()
    {
        if (Fsm.CurrentState == DeathState)
            return;

        Grounded = Physics2D.OverlapCircle(groundCheck.position, Data.GroundCheckRadius, groundLayer);

        // 상태의 FixedUpdate 에서 player.ShouldFall 을 읽으므로 그 전에 갱신해 둔다
        if (Grounded) notGroundedFrames = 0;
        else          notGroundedFrames++;

        Fsm.FixedUpdate();

        if (Fsm.CurrentState == DodgeState || Fsm.CurrentState == DeathState)
            return;

        Move(move);

        if (Grounded)
        {
            jumpCount = 0;
            coyoteCounter = Data.CoyoteTime;

            if (CommandQueue.Count > 0 && CommandQueue.Peek() == PlayerCommand.Down)
                CommandQueue.Dequeue();
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

        bool isAttacking = Fsm.CurrentState == AttackState;
        bool isHit = Fsm.CurrentState == HitState;

        if (jumpBufferCounter > 0f && (coyoteCounter > 0f || jumpCount < Data.MaxJumpCount))
        {
            Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, Data.JumpPower);

            if (!isAttacking && !isHit && Fsm.CurrentState != JumpState)
                Fsm.ChangeState(JumpState);

            jumpCount++;
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        if (isAttacking || isHit)
        {
            if (!JumpHeld && Rb.linearVelocity.y > 0f)
            {
                Rb.linearVelocity += (Data.LowJumpMultiplier - 1f)
                    * Physics2D.gravity.y * Time.fixedDeltaTime * Vector2.up;
            }

            if (Rb.linearVelocity.y < 0f)
            {
                Rb.linearVelocity += (Data.FallMultiplier - 1f)
                    * Physics2D.gravity.y * Time.fixedDeltaTime * Vector2.up;
            }
        }

        // 낙하 전이는 각 지상 상태(Idle/Crouch/Parry)의 FixedUpdate 에서 player.ShouldFall 로 처리한다.
    }

    public void Move(Vector2 move)
    {
        if (move.x < 0f)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
            Animator.SetBool(MoveBool, true);
        }
        else if (move.x > 0f)
        {
            transform.localScale = new Vector3(1f, 1f, 1f);
            Animator.SetBool(MoveBool, true);
        }
        else
        {
            Animator.SetBool(MoveBool, false);

            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        float speedMultiplier = Fsm.CurrentState == CrouchState ? Data.CrouchSpeedMultiplier : 1f;
        transform.position += Data.MoveSpeed * speedMultiplier * Time.fixedDeltaTime * new Vector3(move.x, 0f);
    }

    /// <summary>
    /// 공격·패링 등이 끝났을 때 호출. 지상이면 Idle, 공중이면 Fall 로 전이한다.
    /// (공중에서 행동이 끝났는데 Idle 로 가서 Move 애니메이션이 재생되는 "공중 걷기"를 방지)
    /// </summary>
    public void ChangeToNeutralState()
        => Fsm.ChangeState(Grounded ? IdleState : FallState);

    private void OnJump(InputAction.CallbackContext context)
    {
        if (Fsm.CurrentState == HitState || Fsm.CurrentState == DodgeState) return;

        if (context.performed)
        {
            JumpHeld = true;
            jumpBufferCounter = Data.JumpBufferTime;
        }

        if (context.canceled)
            JumpHeld = false;
    }

    private void OnAttack(InputAction.CallbackContext _)
    {
        if (Fsm.CurrentState == HitState || Fsm.CurrentState == DodgeState) return;

        if (!Grounded && CommandQueue.Count > 0 && CommandQueue.Peek() == PlayerCommand.Down &&
            (Fsm.CurrentState == JumpState || Fsm.CurrentState == FallState || Fsm.CurrentState == AttackState || Fsm.CurrentState == IdleState))
        {
            CommandQueue.Clear();
            Fsm.ChangeState(PlungeState);
            return;
        }

        if (Fsm.CurrentState == AttackState)
        {
            if (CommandQueue.Count == 0) CommandQueue.Enqueue(PlayerCommand.Attack);
            return;
        }

        Fsm.ChangeState(AttackState);
    }

    public void AttackStart()
    {
        attackZone.Activate();
        if (Fsm.CurrentState == AttackState) IsAttackEnd = false;
    }

    public void AttackEnd()
    {
        attackZone.Deactivate();
        if (Fsm.CurrentState == AttackState) IsAttackEnd = true;
    }

    public IDamageable.DamageInfo SetDamage() => new() { canParry = false, damage = Data.Atk };

    public void GetDamage(IDamageable.DamageInfo damageInfo)
    {
        if (invincible && !damageInfo.ignoreInvincible)
        {
            return;
        }

        if (parrying && damageInfo.canParry)
        {
            SuccessParry?.Invoke();
            if (parryEffect)
            {
                parryEffect.transform.position = transform.position;
                parryEffect.Play();
            }
            return;
        }


        KnockbackDir = damageInfo.knockbackDir;
        currHp -= damageInfo.damage;

        OnHpChange?.Invoke(currHp, Data.MaxHp);

        if (currHp <= 0)
        {
            currHp = 0;
            Fsm.ChangeState(DeathState);
            return;
        }
        
        Fsm.ChangeState(HitState);

        OnHit?.Invoke();
    }

    private void OnDodge(InputAction.CallbackContext _)
    {
        if (Fsm.CurrentState == HitState || dodgeCool < Data.DodgeCooldown) return;
        dodgeCool = 0f;
        Fsm.ChangeState(DodgeState);
    }

    private void OnParry(InputAction.CallbackContext _)
    {
        if (Fsm.CurrentState == HitState || Fsm.CurrentState == DodgeState) return;
        Fsm.ChangeState(ParryState);
    }

    private void OnPause(InputAction.CallbackContext _)
    {
        GamePause?.Invoke();
    }

    // 애니메이션 이벤트용 스텁. 콤보 입력 윈도우 표시가 원래 의도였으나
    // 현재 어떤 상태도 이 값을 참조하지 않아 게이팅은 비활성 상태다.
    // (메서드 자체는 .anim 이벤트가 참조하므로 남겨둔다.)
    public void OpenInputQueue() { }

    public void CloseInputQueue() { }

    public void ToggleInvincible() => invincible = !invincible;

    public void ToggleParry() => parrying = !parrying;

    public void ResetAttackEnd() => IsAttackEnd = false;

    public void EnableEffect()
    {
        effect1.Play();
    }
    public void EnableEffect2()
    {
        effect2.Play();
    }

    public void EnableEffect3()
    {
        effect3.Play();
    }

    public void EnableDodgeEffect()
    {
        dodgeAttackEffect.Play();
    }

    public void EnablePlungeEffect()
    {
        plungeEffect.Play();
    }

    private void OnDown(InputAction.CallbackContext context)
    {
        if (Fsm.CurrentState == HitState) return;

        if (context.performed)
        {
            // 공중이면 큐 초기화 후 낙하 공격 버퍼 (대쉬 중에도 허용)
            if (!Grounded)
            {
                CommandQueue.Clear();
                CommandQueue.Enqueue(PlayerCommand.Down);
            }
            // 지상이면 크라우칭 (대쉬 중엔 크라우칭 제외)
            else if (Grounded && Fsm.CurrentState != CrouchState && Fsm.CurrentState != DodgeState)
                Fsm.ChangeState(CrouchState);
        }

        if (context.canceled && Fsm.CurrentState == CrouchState)
            Fsm.ChangeState(IdleState);
    }

    public void SetColliderCrouch(bool crouch)
    {
        if (boxcollider == null) return;
        if (crouch)
        {
            boxcollider.size = new Vector2(boxcollider.size.x, Data.CrouchColliderHeight);
            boxcollider.offset = new Vector2(boxcollider.offset.x, Data.CrouchColliderOffsetY);
        }
        else
        {
            boxcollider.size = new Vector2(boxcollider.size.x, Data.StandColliderHeight);
            boxcollider.offset = new Vector2(boxcollider.offset.x, Data.StandColliderOffsetY);
        }
    }

    public void PlayLandSound()
    {
        SoundManager.Instance.PlaySFX(LandSound, 3f);
    }
}