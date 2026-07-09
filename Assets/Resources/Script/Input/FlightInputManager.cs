using UnityEngine;

public class FlightInputManager : MonoBehaviour
{
    public static FlightInputManager Instance { get; private set; }

    [Header("Current Input")]
    public float TurnInput { get; private set; }     // -1 ~ 1
    public float BlowInput { get; private set; }     // 0 ~ 1
    public bool LaunchPressed { get; private set; }  // 발사 버튼

    private IInputProvider inputProvider;

    private void Awake()
    {
        // 싱글톤
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

#if UNITY_EDITOR
        inputProvider = new PCInputProvider();
#else
        inputProvider = new QuestInputProvider();
#endif
    }

    private void Update()
    {
        inputProvider.UpdateInput();

        TurnInput = inputProvider.TurnInput;
        BlowInput = inputProvider.BlowInput;
        LaunchPressed = inputProvider.LaunchPressed;
    }
}