using UnityEngine;
using UnityEngine.InputSystem;

public class BreathDetector : MonoBehaviour
{
    public static BreathDetector Instance { get; private set; }

    public float BreathPower { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Update()
    {
#if UNITY_EDITOR
        // PC 테스트용
        if (Keyboard.current.spaceKey.isPressed)
            BreathPower = 1f;
        else if (Keyboard.current.wKey.isPressed)
            BreathPower = 0.5f;
        else
            BreathPower = 0f;
#else
        // Quest는 다음 단계에서 구현
        BreathPower = 0f;
#endif
    }
}