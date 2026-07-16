public interface IInputProvider
{
    float TurnInput { get; }

    float BlowInput { get; }

    bool LaunchPressed { get; }

    void UpdateInput();
}