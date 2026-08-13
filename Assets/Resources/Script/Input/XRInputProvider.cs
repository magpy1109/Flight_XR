using UnityEngine;

public class XRInputProvider : IInputProvider
{
    public float TurnInput { get; private set; }

    public float BlowInput { get; private set; }

    public bool LaunchPressed { get; private set; }

    private FlightInputActions actions;

    public XRInputProvider()
    {
        actions = new FlightInputActions();
        actions.Enable();
    }

    public void UpdateInput()
    {
        TurnInput =
            actions.Flight.Turn.ReadValue<float>();

        LaunchPressed =
            actions.Flight.Launch.WasPressedThisFrame();

        if (BreathDetector.Instance != null)
            BlowInput = BreathDetector.Instance.BreathPower;
        else
            BlowInput = 0;
    }
}