using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour, IDamageable
{
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
    public bool JumpHeld { get; internal set; } = false;
    public bool Grounded { get; internal set; }
    public bool IsAttackEnd { get; private set; } = false;
    public Vector2 KnockbackDir => combat.KnockbackDir;

    /// <summary>
    /// 낙하 상태로 전이해야 하는지 여부. 착지 직후 grace 프레임을 넘긴 뒤 하강 중일 때 true.
    /// 지상에 설 수 있는 상태(Idle/Crouch/Parry)의 FixedUpdate 에서 이 값으로 FallState 로 전이한다.
    /// </summary>
    public bool ShouldFall => movement.ShouldFall;

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

    private PlayerMovementController movement;
    private PlayerInputController input;
    private PlayerCombat combat;

    private void Awake()
    {
        Animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        var boxcollider = GetComponent<BoxCollider2D>();
        attackZone.gameObject.SetActive(false);
        OriginalGravityScale = rb.gravityScale;
        AfterImage = GetComponent<DashAfterImage>();
        sr = GetComponent<SpriteRenderer>();
        downAttackZone.gameObject.SetActive(false);

        movement = new PlayerMovementController(this, groundCheck, groundLayer, boxcollider);
        input    = new PlayerInputController(this, movement);
        combat   = new PlayerCombat(this, parryEffect);
    }

    private void OnEnable()
    {
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

        input.Bind();
        combat.Initialize();
    }

    private void OnDisable()
    {
        input.Unbind();
    }

    private void Start() => Fsm.ChangeState(IdleState);

    public void OnMove(InputAction.CallbackContext context)
        => movement.HandleMoveInput(context.ReadValue<Vector2>());

    private void Update()
    {
        if (Fsm.CurrentState == DeathState)
            return;

        input.Tick(Time.deltaTime);

        Fsm.Update();
    }

    private void FixedUpdate()
    {
        if (Fsm.CurrentState == DeathState)
            return;

        movement.FixedUpdateTick();
    }

    public void Move(Vector2 move) => movement.Move(move);

    public void ChangeToNeutralState()
        => Fsm.ChangeState(Grounded ? IdleState : FallState);

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

    public IDamageable.DamageInfo SetDamage() => combat.SetDamage();

    public void GetDamage(IDamageable.DamageInfo damageInfo) => combat.GetDamage(damageInfo);

    public void ToggleInvincible() => combat.ToggleInvincible();

    public void ToggleParry() => combat.ToggleParry();

    public void OpenInputQueue() { }

    public void CloseInputQueue() { }

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

    public void SetColliderCrouch(bool crouch) => movement.SetColliderCrouch(crouch);

    public void PlayLandSound()
    {
        SoundManager.Instance.PlaySFX(LandSound, 3f);
    }
}
