using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class BossController : MonoBehaviour, IDamageable
{
    public FSM Fsm { get; private set; }
    public Animator Animator { get; private set; }
    public float PlayerDistance { get; private set; }
    public int CurrHp { get; protected set; }
    public bool IsAttack { get; set; }
    public DecideState DecideState { get; protected set; }
    [SerializeField] private Transform lookAtZone;
    public Transform LookAtZone => lookAtZone;

    [SerializeField] private BossData data;
    public BossData Data => data;

    [SerializeField] private AttackZone attackZone;
    private SpriteRenderer spriteRenderer;

    [Header("── 피격 효과 ──")]
    [SerializeField] private float hitFlashDuration = 0.08f;
    [SerializeField] private float hitStopDuration = 0.04f;

    [Header("── 사망 연출 ──")]
    [SerializeField] private float deathStopDuration = 0.3f;
    [SerializeField] private float deathSlowScale = 0.2f;
    [SerializeField] private float deathSlowDuration = 1.0f;
    [SerializeField] private ParticleSystem deathParticle;  // 없어도 작동

    public bool IsDead { get; protected set; } = false;
    public bool IsGameOver { get; protected set; } = false;

    private Transform player;
    private int maxHp;

    private void Awake()
    {
        Animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        DecideState = new DecideState(this);
        attackZone.Deactivate();
        OnAwake();
    }

    private void Start()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        maxHp = data.Hp;
        CurrHp = maxHp;
        Fsm = new FSM();
        InitStates();
    }

    protected virtual void OnAwake() { }

    protected virtual void Update()
    {
        PlayerDistance = Vector3.Distance(transform.position, player.position);
        if (!IsAttack)
        {
            transform.localScale = player.position.x < transform.position.x
                ? new Vector3(-1f, 1f, 1f)
                : new Vector3(1f, 1f, 1f);
        }

        Fsm.Update();
    }

    protected virtual void FixedUpdate()
    {
        Fsm.FixedUpdate();
    }

    protected abstract void InitStates();
    public abstract IState ChooseNextAction();

    public void StartAttack()   => IsAttack = true;
    public void EndAttack()     => IsAttack = false;
    public void OnAttackZone()  => attackZone.Activate();
    public void OffAttackZone() => attackZone.Deactivate();

    public abstract IDamageable.DamageInfo SetDamage();

    public virtual void GetDamage(IDamageable.DamageInfo damageInfo)
    {
        CurrHp -= damageInfo.damage;
        _ = HitFlash();
        _ = TimeControl.HitStop(0.05f, hitStopDuration);
    }

    protected void TriggerDeathEffect() => _ = DeathEffect();

    private async UniTask HitFlash()
    {
        if (!spriteRenderer) return;
        Color current = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        await UniTask.Delay(Mathf.RoundToInt(hitFlashDuration * 1000), ignoreTimeScale: true);
        spriteRenderer.color = current;
    }

    private async UniTask DeathEffect()
    {
        int token = TimeControl.Claim(0f);
        await UniTask.Delay(Mathf.RoundToInt(deathStopDuration * 1000), ignoreTimeScale: true);
        if (!TimeControl.IsOwner(token)) return;

        TimeControl.Set(token, deathSlowScale);
        if (deathParticle) deathParticle.Play();

        float elapsed = 0f;
        while (elapsed < deathSlowDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            await UniTask.Yield(PlayerLoopTiming.LastUpdate);
        }

        TimeControl.Release(token);
    }
}
