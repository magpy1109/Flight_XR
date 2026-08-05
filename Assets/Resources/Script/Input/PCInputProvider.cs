using UnityEngine;

public class PCInputProvider : IInputProvider
{
    public float TurnInput { get; private set; }

    public float BlowInput { get; private set; }

    public bool LaunchPressed { get; private set; }

    public void UpdateInput()
    {
        //-------------------------
        // 좌우 조종
        //-------------------------
        TurnInput = Input.GetAxisRaw("Horizontal");

        //-------------------------
        // 입김 테스트
        //-------------------------
        BlowInput = BreathDetector.Instance.BreathPower;

        //-------------------------
        // 발사
        //-------------------------
        LaunchPressed = Input.GetKeyDown(KeyCode.Q);
    }
}