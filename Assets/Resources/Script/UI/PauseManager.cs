using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    public static bool IsPaused { get; private set; }

    private static int lastResumeFrame = -10;

    // 일시정지 중 + 재개 직후 2프레임 동안 게임 입력 차단
    public static bool BlockGameInput =>
        IsPaused || Time.frameCount - lastResumeFrame < 2;

    [Header("UI")]
    [SerializeField] private Canvas pauseCanvas;   // 월드 스페이스 Canvas
    [SerializeField] private float distance = 1.5f;
    [SerializeField] private float heightOffset = -0.1f;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    private InputAction pauseAction;
    private QuestUIRayInteractor rayInteractor;

    private void Awake()
    {
        Instance = this;
        IsPaused = false;
        lastResumeFrame = -10;

        pauseAction = new InputAction("Pause", InputActionType.Button);
        pauseAction.AddBinding("<XRController>{RightHand}/secondaryButton"); // B
#if UNITY_EDITOR
        pauseAction.AddBinding("<Keyboard>/escape");
#endif
    }

    private void OnEnable()  => pauseAction.Enable();
    private void OnDisable() => pauseAction.Disable();

    private void OnDestroy()
    {
        pauseAction.Dispose();

        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private void Update()
    {
        if (!pauseAction.WasPressedThisFrame())
            return;

        if (IsPaused)
        {
            Resume();
            return;
        }

        // 결과창이 떠 있을 때는 일시정지 불가
        if (ResultUI.Instance != null && ResultUI.Instance.IsResultShown)
            return;

        Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;

        Camera cam = Camera.main;
        if (cam == null || pauseCanvas == null) return;

        Transform head = cam.transform;

        // 누른 순간의 정면(수평 방향) 1.5m 앞에 배치하고 이후엔 고정
        Vector3 fwd = head.forward;
        fwd.y = 0f;
        fwd = fwd.sqrMagnitude < 0.001f ? Vector3.forward : fwd.normalized;

        Vector3 pos = head.position + fwd * distance;
        pos.y += heightOffset;

        pauseCanvas.transform.SetPositionAndRotation(
            pos, Quaternion.LookRotation(fwd));
        pauseCanvas.worldCamera = cam;

        // 활성화를 먼저 해야 레이가 버튼을 찾을 수 있음
        pauseCanvas.gameObject.SetActive(true);

        if (rayInteractor == null)
            rayInteractor = FindFirstObjectByType<QuestUIRayInteractor>();
        if (rayInteractor != null)
            rayInteractor.SetTargetCanvas(pauseCanvas);

        IsPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    public void Resume()
    {
        if (!IsPaused) return;

        // 캔버스를 끄기 전에 레이를 되돌려야 버튼 hover 상태가 정리됨
        if (rayInteractor != null)
            rayInteractor.RestoreDefaultCanvas();

        if (pauseCanvas != null)
            pauseCanvas.gameObject.SetActive(false);

        IsPaused = false;
        lastResumeFrame = Time.frameCount;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    // ---- 버튼 OnClick 연결용 ----

    public void OnClickResume() => Resume();

    public void OnClickRestart()
    {
        Resume();

        // 카운트다운 도중이었다면 취소해야 StartCountdown이 막히지 않음
        if (CountdownManager.Instance != null)
            CountdownManager.Instance.CancelCountdown();

        // 결과창의 재시작과 동일: 초기화 → 카운트다운 → 발사
        if (ResultUI.Instance != null)
            ResultUI.Instance.RestartGame();
    }

    public void OnClickExit()
    {
        Resume();

        if (CountdownManager.Instance != null)
            CountdownManager.Instance.CancelCountdown();

        if (ResultUI.Instance != null)
            ResultUI.Instance.GoHome();   // GameCanvas 끄고 씬 이동
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }
}