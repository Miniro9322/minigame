/// <summary>
/// Boss1 FSM 상태 공통 베이스.
/// boss 참조 보관 + 안 쓰는 콜백을 빈 가상 메서드로 제공한다.
/// </summary>
public abstract class BossState : IState
{
    protected readonly Boss1 boss;

    protected BossState(Boss1 boss) => this.boss = boss;

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
}
