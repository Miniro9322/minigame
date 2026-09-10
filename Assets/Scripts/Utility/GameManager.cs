using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private BossController boss;

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
        debug     = InputSystem.actions.FindAction("DebugMode");
        debugOff  = InputSystem.actions.FindAction("OffDebug");
        rush      = InputSystem.actions.FindAction("Rush");

        attack.performed   += OnAttack;
        attack2.performed  += OnAttack2;
        rush.performed     += OnRush;
        debug.performed    += OnDebug;
        debugOff.performed += OffDebug;
    }

    private void OnDisable()
    {
        attack.performed   -= OnAttack;
        attack2.performed  -= OnAttack2;
        debug.performed    -= OnDebug;
        debugOff.performed -= OffDebug;
        rush.performed     -= OnRush;
    }

    // 패링 성공 시 호출(SuccessParry UnityEvent). timeScale 0.1 로 약 1초(실시간) 정지 연출.
    // 기존 코루틴이 WaitForSeconds(0.1) 을 느린 시간축에서 대기하던 동작을 그대로 유지.
    public void HitStop() => _ = TimeControl.HitStop(0.1f, 1.0f);

    public void OnGameOver()
    {
        playerInput.actions.FindActionMap("Player").Disable();
    }

    private void OnRush(InputAction.CallbackContext _)   => boss.SendMessage("Rush");
    private void OnAttack(InputAction.CallbackContext _)  => boss.SendMessage("Attack1");
    private void OnAttack2(InputAction.CallbackContext _) => boss.SendMessage("Attack2");

    private void OnDebug(InputAction.CallbackContext _)
    {
        Debug.Log("디버그 모드");
        playerInput.actions.FindActionMap("UI").Disable();
        playerInput.SwitchCurrentActionMap("Debug");
    }

    private void OffDebug(InputAction.CallbackContext _)
    {
        Debug.Log("디버그 모드 오프");
        playerInput.SwitchCurrentActionMap("Player");
        playerInput.actions.FindActionMap("UI").Enable();
    }
}
