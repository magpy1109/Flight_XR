using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;

public class GameResultManager : MonoBehaviour
{
    public static GameResultManager Instance;

    private FirebaseFirestore db;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
    }

    public void SaveResult(
        int score,
        float distance,
        int flightTime,
        float maxHeight,
        int ringCount)
        
    {
        Debug.Log("SaveResult 호출됨");
        
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;

        if (user == null)
        {
            Debug.LogError("로그인이 되어있지 않습니다.");
            return;
        }

        GameResult result = new GameResult();

        result.user_id = user.UserId;
        result.score = score;
        result.distance = distance;
        result.flight_time = flightTime;
        result.max_height = maxHeight;
        result.ring_count = ringCount;

        Debug.Log("Firestore 저장 시작");

        db.Collection("game_results")
            .AddAsync(result)
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompletedSuccessfully)
                {
                    Debug.LogError("게임 결과 저장 실패");
                    return;
                }

                Debug.Log("게임 결과 저장 완료");

                StatsManager.Instance.UpdateStats(result);
            });
    }
}