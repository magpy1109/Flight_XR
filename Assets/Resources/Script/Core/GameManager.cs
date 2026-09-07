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
    if (FlightInputManager.Instance == null)
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
            GameResultManager.Instance.SaveResult(
                Score,
                Distance,
                flightTime,
                MaxHeight,
                RingCount);
        }

        Destroy(plane);
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