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
        if (FlightInputManager.Instance.LaunchPressed)
        {
            launcher.Launch();

            //StartGame();
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

    public void EndGame()
    {
        if (!IsPlaying)
            return;

        IsPlaying = false;

        int flightTime =
            Mathf.RoundToInt(Time.time - startTime);

        GameResultManager.Instance.SaveResult(
            Score,
            Distance,
            flightTime,
            MaxHeight,
            RingCount);

        Debug.Log("게임 종료");
    }

    public void AddScore(int value)
    {
        Score += value;
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