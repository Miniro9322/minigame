using UnityEngine.InputSystem;

/// <summary>플레이어 입력 액션의 바인딩/언바인딩과 각 입력에 대한 FSM 전이 판단을 전담한다.</summary>
public class PlayerInputController
{
    private readonly Player player;
    private readonly PlayerMovementController movement;

    private InputAction jump;
    private InputAction attack;
    private InputAction dodge;
    private InputAction parry;
    private InputAction pause;
    private InputAction down;

    private float dodgeCool = 0f;

    public PlayerInputController(Player player, PlayerMovementController movement)
    {
        this.player   = player;
        this.movement = movement;
    }

    public void Bind()
    {
        jump   = InputSystem.actions.FindAction("Jump");
        attack = InputSystem.actions.FindAction("Attack");
        dodge  = InputSystem.actions.FindAction("Dodge");
        parry  = InputSystem.actions.FindAction("Parry");
        pause  = InputSystem.actions.FindAction("Pause");
        down   = InputSystem.actions.FindAction("Down");

        jump.performed   += OnJump;
        jump.canceled    += OnJump;
        attack.performed += OnAttack;
        dodge.performed  += OnDodge;
        parry.performed  += OnParry;
        pause.performed  += OnPause;
        down.performed   += OnDown;
        down.canceled    += OnDown;

        dodgeCool = player.Data.DodgeCooldown;
    }

    public void Unbind()
    {
        jump.performed   -= OnJump;
        jump.canceled    -= OnJump;
        attack.performed -= OnAttack;
        dodge.performed  -= OnDodge;
        parry.performed  -= OnParry;
        down.performed   -= OnDown;
        down.canceled    -= OnDown;
        pause.performed  -= OnPause;
    }

    public void Tick(float deltaTime)
    {
        if (dodgeCool < player.Data.DodgeCooldown)
            dodgeCool += deltaTime;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (player.Fsm.CurrentState == player.HitState || player.Fsm.CurrentState == player.DodgeState) return;

        if (context.performed)
        {
            player.JumpHeld = true;
            movement.RequestJumpBuffer();
        }

        if (context.canceled)
            player.JumpHeld = false;
    }

    private void OnAttack(InputAction.CallbackContext _)
    {
        if (player.Fsm.CurrentState == player.HitState || player.Fsm.CurrentState == player.DodgeState) return;

        if (!player.Grounded && player.CommandQueue.Count > 0 && player.CommandQueue.Peek() == PlayerCommand.Down &&
            (player.Fsm.CurrentState == player.JumpState || player.Fsm.CurrentState == player.FallState ||
             player.Fsm.CurrentState == player.AttackState || player.Fsm.CurrentState == player.IdleState))
        {
            player.CommandQueue.Clear();
            player.Fsm.ChangeState(player.PlungeState);
            return;
        }

        if (player.Fsm.CurrentState == player.AttackState)
        {
            if (player.CommandQueue.Count == 0) player.CommandQueue.Enqueue(PlayerCommand.Attack);
            return;
        }

        player.Fsm.ChangeState(player.AttackState);
    }

    private void OnDodge(InputAction.CallbackContext _)
    {
        if (player.Fsm.CurrentState == player.HitState || dodgeCool < player.Data.DodgeCooldown) return;
        dodgeCool = 0f;
        player.Fsm.ChangeState(player.DodgeState);
    }

    private void OnParry(InputAction.CallbackContext _)
    {
        if (player.Fsm.CurrentState == player.HitState || player.Fsm.CurrentState == player.DodgeState) return;
        player.Fsm.ChangeState(player.ParryState);
    }

    private void OnPause(InputAction.CallbackContext _)
    {
        player.GamePause?.Invoke();
    }

    private void OnDown(InputAction.CallbackContext context)
    {
        if (player.Fsm.CurrentState == player.HitState) return;

        if (context.performed)
        {
            if (!player.Grounded)
            {
                player.CommandQueue.Clear();
                player.CommandQueue.Enqueue(PlayerCommand.Down);
            }
            else if (player.Grounded && player.Fsm.CurrentState != player.CrouchState && player.Fsm.CurrentState != player.DodgeState)
                player.Fsm.ChangeState(player.CrouchState);
        }

        if (context.canceled && player.Fsm.CurrentState == player.CrouchState)
            player.Fsm.ChangeState(player.IdleState);
    }
}
