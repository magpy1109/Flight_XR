using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance;

    private FirebaseFirestore db;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private System.Collections.IEnumerator Start()
    {
        // Firebase 초기화 완료 대기
        while (
            FirebaseManager.Instance == null ||
            !FirebaseManager.Instance.IsInitialized)
        {
            yield return null;
        }

        Debug.Log("StatsManager : Firebase 초기화 확인");

        db = FirebaseFirestore.DefaultInstance;
    }

    public void UpdateStats(GameResult result)
    {
        if (db == null)
        {
            Debug.LogError(
                "StatsManager : Firestore가 초기화되지 않았습니다."
            );
            return;
        }

        if (result == null)
        {
            Debug.LogError("StatsManager : GameResult가 null입니다.");
            return;
        }

        DocumentReference doc =
            db.Collection("user_stats")
            .Document(result.user_id);

        doc.GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError(
                        "스탯 조회 실패 : " +
                        task.Exception
                    );
                    return;
                }

                if (!task.Result.Exists)
                {
                    Debug.LogError("user_stats 없음");
                    return;
                }

                UserStats stats =
                    task.Result.ConvertTo<UserStats>();

                // -------------------------
                // 누적 데이터
                // -------------------------

                stats.play_count++;

                stats.total_score += result.score;

                stats.total_distance += result.distance;

                stats.total_play_time += result.flight_time;

                stats.total_height += result.max_height;

                // -------------------------
                // 최고 기록
                // -------------------------

                if (result.score > stats.best_score)
                {
                    stats.best_score = result.score;
                }

                if (result.distance > stats.best_distance)
                {
                    stats.best_distance = result.distance;
                }

                // -------------------------
                // 업데이트 시간
                // -------------------------

                stats.updated_at =
                    Timestamp.GetCurrentTimestamp();

                Debug.Log(
                    $"스탯 저장 준비 | " +
                    $"Score={stats.total_score}, " +
                    $"Distance={stats.total_distance}, " +
                    $"PlayCount={stats.play_count}"
                );

                // Firestore 저장
                doc.SetAsync(stats)
                    .ContinueWithOnMainThread(saveTask =>
                    {
                        if (
                            saveTask.IsCanceled ||
                            saveTask.IsFaulted
                        )
                        {
                            Debug.LogError(
                                "스탯 저장 실패 : " +
                                saveTask.Exception
                            );
                            return;
                        }

                        Debug.Log("스탯 업데이트 완료");

                        // 메모리에 가지고 있는 값도 갱신
                        if (SaveManager.Instance != null)
                            SaveManager.Instance.UpdateLocalStats(stats);

                        if (SkinUnlockManager.Instance != null)
                            SkinUnlockManager.Instance.CheckUnlocks(stats);
                    });
            });
    }
}