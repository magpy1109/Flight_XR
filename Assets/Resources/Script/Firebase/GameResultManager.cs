using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;

/// <summary>
/// 한 판의 결과를 Firestore game_results에 저장하고, user_stats(최고 기록 / 누적)를 갱신한다.
///
/// game_results 문서 : user_id, score(노란 네모 점수), distance, flight_time, max_height,
///                     cube_count(먹은 네모 수), max_combo(최대 연속), ring_count(폐기, 0), created_at
/// </summary>
public class GameResultManager : MonoBehaviour
{
    public static GameResultManager Instance;

    private FirebaseFirestore db;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private FirebaseFirestore Db
    {
        get
        {
            if (db == null)
                db = FirebaseFirestore.DefaultInstance;
            return db;
        }
    }

    /// <summary>예전 호출 방식 호환</summary>
    public void SaveResult(
        int score,
        float distance,
        int flightTime,
        float maxHeight,
        int ringCount)
    {
        SaveResult(score, distance, flightTime, maxHeight, ringCount, 0, 0);
    }

    public void SaveResult(
        int score,
        float distance,
        int flightTime,
        float maxHeight,
        int ringCount,
        int cubeCount,
        int maxCombo)
    {
        // Firebase를 쓸 수 없으면(설정 파일 누락 / 초기화 전) 서버 저장 생략 (예전에는 여기서 예외가 났음)
        if (!FirebaseManager.Ready)
        {
            Debug.LogWarning("[GameResultManager] Firebase가 준비되지 않아 기록을 서버에 저장하지 않습니다.");
            return;
        }

        FirebaseUser user;
        try
        {
            user = FirebaseAuth.DefaultInstance.CurrentUser;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[GameResultManager] Firebase 로그인 정보를 읽지 못했습니다 : " + e.Message);
            return;
        }

        if (user == null)
        {
            Debug.LogError("[GameResultManager] 로그인이 되어있지 않아 기록을 저장하지 않습니다.");
            return;
        }

        GameResult result = new GameResult();

        result.user_id = user.UserId;
        result.score = score;
        result.distance = distance;
        result.flight_time = flightTime;
        result.max_height = maxHeight;
        result.ring_count = ringCount;
        result.cube_count = cubeCount;
        result.max_combo = maxCombo;

        Debug.Log($"[GameResultManager] 저장 시작 | 점수={score} 거리={distance:F1} 네모={cubeCount} 최대연속={maxCombo}");

        Db.Collection("game_results")
            .AddAsync(result)
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompletedSuccessfully)
                {
                    Debug.LogError("[GameResultManager] 게임 결과 저장 실패 : " + task.Exception);
                    return;
                }

                Debug.Log("[GameResultManager] 게임 결과 저장 완료");

                if (StatsManager.Instance != null)
                    StatsManager.Instance.UpdateStats(result);
                else
                    Debug.LogError("[GameResultManager] StatsManager가 없어 최고 기록을 갱신하지 못했습니다.");
            });
    }
}
