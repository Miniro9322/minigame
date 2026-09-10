public class FSM
{
    public IState CurrentState { get; private set; }

    public void ChangeState(IState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState?.Enter();
    }

    public bool IsInState<T>() where T : IState => CurrentState is T;

    public void Update() => CurrentState?.Update();

    public void FixedUpdate() => CurrentState?.FixedUpdate();
}
