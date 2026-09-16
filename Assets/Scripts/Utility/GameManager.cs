using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private BossController boss;

    [Header("── 연출용 히트스탑 (애니메이션 이벤트에서 호출) ──")]
    [Tooltip("낮출 timeScale 값")]
    [SerializeField] private float hitStopScale = 0.1f;
    [Tooltip("지속 시간. unscaled 기준")]
    [SerializeField] private float hitStopDuration = 1.0f;

    private InputAction debug;
    private InputAction debugOff;
    private InputAction attack;
    private InputAction attack2;
    private InputAction rush;

    private void Start()
    {
        playerInput.SwitchCurrentActionMap("Player");
        playerInput.actions.FindActionMap("Debug").Disable();
    }

    private void OnEnable()
    {
        attack    = InputSystem.actions.FindAction("BossAttack");
        attack2   = InputSystem.actions.FindAction("BossAttack2");
        rush      = InputSystem.actions.FindAction("Rush");

        attack.performed   += OnAttack;
        attack2.performed  += OnAttack2;
        rush.performed     += OnRush;
    }

    private void OnDisable()
    {
        attack.performed   -= OnAttack;
        attack2.performed  -= OnAttack2;
        rush.performed     -= OnRush;
    }

    public void HitStop() => _ = TimeControl.HitStop(hitStopScale, hitStopDuration);

    public void OnGameOver()
    {
        playerInput.actions.FindActionMap("Player").Disable();
    }

    private void OnRush(InputAction.CallbackContext _)   => boss.SendMessage("Rush");
    private void OnAttack(InputAction.CallbackContext _)  => boss.SendMessage("Attack1");
    private void OnAttack2(InputAction.CallbackContext _) => boss.SendMessage("Attack2");
}
