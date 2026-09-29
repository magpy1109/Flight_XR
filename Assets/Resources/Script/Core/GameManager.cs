using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Plane")]
    [SerializeField] private PlaneLauncher launcher;

    public bool IsPlaying { get; private set; }

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
            Destroy(gameObject);
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

        int flightTime =
            Mathf.RoundToInt(Time.time - startTime);

        Debug.Log("=== GAME OVER ===");

        // 결과 UI를 먼저 표시
        if (ResultUI.Instance != null)
        {
            Debug.Log("ResultUI.ShowResult 호출");

            ResultUI.Instance.ShowResult(
                Score,
                Distance,
                MaxHeight,
                RingCount);
        }
        else
        {
            Debug.LogError("ResultUI.Instance가 NULL입니다.");
        }

        // Firebase 저장
        if (GameResultManager.Instance != null)
        {
            Debug.Log("GameResultManager 발견 → SaveResult 호출");

            GameResultManager.Instance.SaveResult(
                Score,
                Distance,
                flightTime,
                MaxHeight,
                RingCount);
        }
        else
        {
            Debug.LogError(
                "GameResultManager.Instance가 NULL입니다."
            );
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

    public void ResetGame()
    {
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