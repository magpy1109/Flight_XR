using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Plane")]
    [SerializeField] private PlaneLauncher launcher;

    public bool IsPlaying { get; private set; }

    [Header("Game Over")]
    [Tooltip("부딪힌 뒤 결과 화면이 뜨기까지 기다리는 시간 (비행기가 찌그러지는 모습을 볼 수 있게)")]
    [SerializeField] private float resultDelay = 1.0f;

    /// <summary>부딪힌 뒤 결과 화면이 뜨기 전까지 (이때는 새 게임 시작 안 됨)</summary>
    public bool IsEnding { get; private set; }

    private Coroutine resultRoutine;
    private int pendingFlightTime;
    private int pendingCubes;
    private int pendingCombo;

    public int Score { get; private set; }

    public float Distance { get; private set; }

    public float MaxHeight { get; private set; }

    public int RingCount { get; private set; }

    private float startTime;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // 공중의 노란 네모 (점수 아이템)
        PathCollectibles.Install(gameObject);
    }

    private void Update()
{
    if (PauseManager.BlockGameInput)
        return;

    if (FlightInputManager.Instance == null)
        return;

    // 결과 화면이 떠 있을 때는 발사 버튼으로 새 게임이 시작되지 않게 (다시도전 버튼 사용)
    if (ResultUI.Instance != null && ResultUI.Instance.IsResultShown)
        return;

    // 부딪힌 뒤 결과 화면이 뜨기 전 (찌그러지는 중)에도 새 게임 시작 안 됨
    if (IsEnding)
        return;

    if (FlightInputManager.Instance.LaunchPressed)
    {
        Debug.Log(
            $"LaunchPressed / IsPlaying = {IsPlaying}");

        if (!IsPlaying)
        {
            CountdownManager.Instance.StartCountdown(() =>
            {
                if (!IsPlaying)
                    launcher.Launch();
            });
        }
    }
}

    public void StartGame()
    {
        if (IsPlaying)
            return;

        IsPlaying = true;

        Score = 0;
        Distance = 0;
        MaxHeight = 0;
        RingCount = 0;

        startTime = Time.time;

        Debug.Log("게임 시작");
    }

    public void EndGame(GameObject plane)
    {
        if (!IsPlaying)
            return;

        IsPlaying = false;

        // 게임오버 진동
        ControllerHaptics.GameOver();

        int flightTime =
            Mathf.RoundToInt(Time.time - startTime);

        Debug.Log("=== GAME OVER ===");

        // 결과 UI는 비행기가 찌그러지는 모습을 잠깐 보여준 뒤 표시
        // Firebase 저장은 결과 화면을 띄운 뒤 (신기록 표시가 방금 저장된 기록과 비교되지 않도록 기존 순서 유지)
        pendingFlightTime = flightTime;
        pendingCubes = PathCollectibles.Instance != null ? PathCollectibles.Instance.Collected : 0;
        pendingCombo = PathCollectibles.Instance != null ? PathCollectibles.Instance.MaxCombo : 0;

        if (resultRoutine != null)
        {
            StopCoroutine(resultRoutine);
            resultRoutine = null;
        }

        if (resultDelay > 0f)
        {
            IsEnding = true;
            resultRoutine = StartCoroutine(ShowResultAfterDelay(Score, Distance, MaxHeight, RingCount));
        }
        else
        {
            ShowResultNow(Score, Distance, MaxHeight, RingCount);
        }

        // 비행기는 바닥에 남겨두고 움직임만 정지
        if (plane != null)
        {
            PlaneController controller =
                plane.GetComponent<PlaneController>();

            if (controller != null)
                controller.enabled = false;

            Rigidbody rb =
                plane.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }

        // 다음 게임에서 새 비행기를 생성할 수 있도록 상태 해제
        if (launcher != null)
        {
            launcher.PlaneFinished(plane);
        }
    }

    private System.Collections.IEnumerator ShowResultAfterDelay(int score, float distance, float maxHeight, int rings)
    {
        // 일시정지 중에는 시간이 멈추므로 결과 화면도 기다림
        yield return new WaitForSeconds(resultDelay);

        resultRoutine = null;
        ShowResultNow(score, distance, maxHeight, rings);
    }

    private void ShowResultNow(int score, float distance, float maxHeight, int rings)
    {
        IsEnding = false;

        if (ResultUI.Instance != null)
        {
            Debug.Log("ResultUI.ShowResult 호출");

            ResultUI.Instance.ShowResult(
                score,
                distance,
                maxHeight,
                rings);
        }
        else
        {
            Debug.LogError("ResultUI.Instance가 NULL입니다.");
        }

        SaveResult(score, distance, pendingFlightTime, maxHeight, rings);
    }

    private void SaveResult(int score, float distance, int flightTime, float maxHeight, int rings)
    {
        if (GameResultManager.Instance != null)
        {
            Debug.Log("GameResultManager 발견 → SaveResult 호출");

            GameResultManager.Instance.SaveResult(
                score,
                distance,
                flightTime,
                maxHeight,
                rings,
                pendingCubes,
                pendingCombo);
        }
        else
        {
            Debug.LogError(
                "GameResultManager.Instance가 NULL입니다."
            );
        }
    }

    private void OnDestroy()
    {
        // 결과 화면이 뜨기 전에 씬을 나가도 기록은 저장
        if (resultRoutine != null)
        {
            resultRoutine = null;
            SaveResult(Score, Distance, pendingFlightTime, MaxHeight, RingCount);
        }
    }

    public void ResetGame()
    {
        if (resultRoutine != null)
        {
            // 결과 화면이 뜨기 전에 재시작되면 기록만 저장
            StopCoroutine(resultRoutine);
            resultRoutine = null;
            SaveResult(Score, Distance, pendingFlightTime, MaxHeight, RingCount);
        }

        IsEnding = false;
        IsPlaying = false;

        Score = 0;
        Distance = 0;
        MaxHeight = 0;
        RingCount = 0;

        Debug.Log("게임 상태 초기화");

        launcher.ResetPlane();
    }

    public void AddScore(int value)
    {
        Score += value;

        Debug.Log($"현재 점수 : {Score}");
    }

    public void AddRing()
    {
        RingCount++;
    }

    public void UpdateDistance(float distance)
    {
        if (distance > Distance)
            Distance = distance;
    }

    public void UpdateHeight(float height)
    {
        if (height > MaxHeight)
            MaxHeight = height;
    }
}