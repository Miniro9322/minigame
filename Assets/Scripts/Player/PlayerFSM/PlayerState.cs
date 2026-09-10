/// <summary>
/// 플레이어 FSM 상태 공통 베이스.
/// player 참조 보관 + 안 쓰는 콜백을 빈 가상 메서드로 제공해 각 상태의 보일러플레이트를 줄인다.
/// </summary>
public abstract class PlayerState : IState
{
    protected readonly Player player;

    protected PlayerState(Player player) => this.player = player;

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
}
