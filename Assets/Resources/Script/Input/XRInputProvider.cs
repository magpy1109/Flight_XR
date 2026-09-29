using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 비행 입력.
/// - 방향 전환 : 오른쪽 컨트롤러 조이스틱 좌우 (감도는 설정의 "조작 감도")
///   예전에는 왼쪽 컨트롤러의 "위치(x)"에 연결되어 있어서, 손 위치에 따라 비행기가 계속 한쪽으로 휘었고
///   게임오버 후 재시작해도 같은 방향으로 휘어 나갔다.
/// - 발사 : 오른쪽 A 버튼 (에디터 : Q)
/// - 상승 : 입김 (BreathDetector)
/// - 에디터 테스트 : 키보드 ← → 또는 A / D
/// </summary>
public class XRInputProvider : IInputProvider
{
    private const float DeadZone = 0.15f;

    public float TurnInput { get; private set; }

    public float BlowInput { get; private set; }

    public bool LaunchPressed { get; private set; }

    private readonly FlightInputActions actions;
    private readonly InputAction stickAction;

    public XRInputProvider()
    {
        actions = new FlightInputActions();
        actions.Enable();

        // 기존 Turn 액션(왼손 위치에 연결됨)은 사용하지 않는다
        actions.Flight.Turn.Disable();

        // 오른쪽 컨트롤러 조이스틱
        stickAction = new InputAction("TurnStick", InputActionType.Value, expectedControlType: "Vector2");
        stickAction.AddBinding("<XRController>{RightHand}/{Primary2DAxis}");
        stickAction.AddBinding("<XRController>{RightHand}/thumbstick");
        stickAction.Enable();
    }

    public void UpdateInput()
    {
        float stick = ReadStickX();

        // 데드존 (손가락을 살짝 얹어도 돌지 않게)
        float magnitude = Mathf.Abs(stick);
        stick = magnitude < DeadZone ? 0f : Mathf.Sign(stick) * (magnitude - DeadZone) / (1f - DeadZone);

        // 에디터 키보드
        stick += ReadKeyboard();

        TurnInput = Mathf.Clamp(stick, -1f, 1f) * TurnSensitivity.Multiplier;

        LaunchPressed =
            actions.Flight.Launch.WasPressedThisFrame();

        if (BreathDetector.Instance != null)
            BlowInput = BreathDetector.Instance.BreathPower;
        else
            BlowInput = 0;
    }

    private float ReadStickX()
    {
        float x = 0f;

        try
        {
            x = stickAction.ReadValue<Vector2>().x;
        }
        catch (System.InvalidOperationException)
        {
            // 조이스틱이 아닌 컨트롤이 연결된 경우 무시
        }

        // Input System에서 값이 안 들어오면 Meta OVRInput으로 한 번 더 확인
        if (Mathf.Abs(x) < 0.01f)
        {
            try
            {
                x = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).x;
            }
            catch (System.Exception)
            {
                // OVRManager가 없는 환경
            }
        }

        return x;
    }

    private static float ReadKeyboard()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return 0f;

        float value = 0f;
        if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) value -= 1f;
        if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) value += 1f;
        return value;
    }
}
