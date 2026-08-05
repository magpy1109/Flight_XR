using UnityEngine;
using UnityEngine.XR;

public class QuestInputProvider : IInputProvider
{
    public float TurnInput { get; private set; }

    public float BlowInput { get; private set; }

    public bool LaunchPressed { get; private set; }

    private InputDevice leftHand;
    private InputDevice rightHand;

    public void UpdateInput()
    {
        if (!leftHand.isValid)
            leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        if (!rightHand.isValid)
            rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        //-------------------
        // 좌우 조종
        //-------------------
        if (leftHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 stick))
        {
            TurnInput = stick.x;
        }

        //-------------------
        // 발사 (A 버튼)
        //-------------------
        LaunchPressed = false;

        if (rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool aButton))
        {
            LaunchPressed = aButton;
        }

        //-------------------
        // 입김
        //-------------------
        if (BreathDetector.Instance != null)
        {
            BlowInput = BreathDetector.Instance.BreathPower;
        }
        else
        {
            BlowInput = 0f;
        }
    }
}