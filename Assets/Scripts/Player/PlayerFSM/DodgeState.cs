using UnityEngine;
using UnityEngine.InputSystem;

public class DodgeState : PlayerState
{
    private static readonly int DodgeHash = Animator.StringToHash("Dodge");

    private readonly PlayerInput playerInput;

    private float dodgeTime;
    private Vector3 dodgeEnd;
    private Vector3 dodgeStart;
    private bool dodgeAttacked = false;
    private bool completedNaturally = false;

    public DodgeState(Player player) : base(player)
    {
        playerInput = player.GetComponent<PlayerInput>();
    }

    public override void Enter()
    {
        player.Animator.Play(DodgeHash);

        dodgeStart = player.transform.position;
        dodgeEnd = new Vector3(
            dodgeStart.x + player.Data.DodgeAmount * player.transform.localScale.x,
            dodgeStart.y);

        player.Rb.linearVelocity = Vector2.zero;
        player.Rb.gravityScale = 0f;

        dodgeTime = 0f;
        completedNaturally = false;

        player.ToggleInvincible();
        player.AfterImage.StartAfterImage();
    }

    public override void Exit()
    {
        if (completedNaturally)
            player.Rb.MovePosition(dodgeEnd);
        player.Rb.linearVelocity = Vector2.zero;
        player.Rb.gravityScale = player.OriginalGravityScale;
        dodgeTime = 0f;
        player.AfterImage.StopAfterImage();
        player.ToggleInvincible();
        if (dodgeAttacked)
        {
            player.AttackEnd();
            player.Animator.Play("Idle", 0, 0f);
        }
        dodgeAttacked = false;
    }

    public override void FixedUpdate()
    {
        if (dodgeTime > player.Data.DodgeDuration)
        {
            completedNaturally = true;
            player.Fsm.ChangeState(player.IdleState);
            return;
        }

        dodgeTime += Time.fixedDeltaTime;

        player.Rb.MovePosition(Vector3.Lerp(dodgeStart, dodgeEnd, dodgeTime / player.Data.DodgeDuration));
    }

    public override void Update()
    {
        if (!dodgeAttacked && dodgeTime < player.Data.DodgeAttackWindow)
        {
            if (playerInput.actions["Attack"].WasPerformedThisFrame())
            {
                player.Animator.Play("DodgeAttack");
                player.EnableDodgeEffect();
                dodgeAttacked = true;
            }
        }
    }
}
