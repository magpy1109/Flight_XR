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

        if (!IsPlaying &&
            FlightInputManager.Instance.LaunchPressed)
        {
            Debug.Log("Q 입력!");

            CountdownManager.Instance.StartCountdown(() =>
            {
                launcher.Launch();
            });
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

        Debug.Log("게임 종료");

        GameResultManager.Instance.SaveResult(
            Score,
            Distance,
            flightTime,
            MaxHeight,
            RingCount);

        // 다음 단계에서 추가 예정
        // StatsManager.Instance.UpdateStats(...);
        // AchievementManager.Instance.Check(...);
        // UIManager.Instance.ShowResult();

        Destroy(plane);
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