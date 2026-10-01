using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;

/// <summary>
/// user_stats(유저별 최고 기록 / 누적 기록) 갱신.
///
/// - 최고 기록 : best_distance(거리 랭킹), best_score(점수 랭킹), best_combo
/// - 누적 : play_count, total_score, total_distance, total_play_time, total_height, total_cubes
/// - 트랜잭션으로 읽고-고쳐-쓰기를 한 번에 처리 (동시에 두 기록이 저장돼도 값이 덮어써지지 않음)
/// - user_stats 문서가 없으면(예전 계정 / 생성 실패) 새로 만든다 (예전에는 오류만 내고 저장 안 됨)
/// </summary>
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
        if (result == null)
        {
            Debug.LogError("StatsManager : GameResult가 null입니다.");
            return;
        }

        if (db == null)
            db = FirebaseFirestore.DefaultInstance;

        DocumentReference doc =
            db.Collection("user_stats")
            .Document(result.user_id);

        db.RunTransactionAsync(transaction =>
        {
            return transaction.GetSnapshotAsync(doc).ContinueWith(snapshotTask =>
            {
                DocumentSnapshot snapshot = snapshotTask.Result;

                UserStats stats = snapshot.Exists
                    ? snapshot.ConvertTo<UserStats>()
                    : new UserStats();

                Apply(stats, result);
                transaction.Set(doc, stats);
                return stats;
            });
        })
        .ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("스탯 저장 실패 : " + task.Exception);
                return;
            }

            UserStats stats = task.Result;

            Debug.Log(
                $"스탯 업데이트 완료 | 최고 거리={stats.best_distance:F1} 최고 점수={stats.best_score} " +
                $"최대 연속={stats.best_combo} 플레이={stats.play_count}");

            // 메모리에 가지고 있는 값도 갱신
            if (SaveManager.Instance != null)
                SaveManager.Instance.UpdateLocalStats(stats);

            if (SkinUnlockManager.Instance != null)
                SkinUnlockManager.Instance.CheckUnlocks(stats);
        });
    }

    private static void Apply(UserStats stats, GameResult result)
    {
        // 누적
        stats.play_count++;
        stats.total_score += result.score;
        stats.total_distance += result.distance;
        stats.total_play_time += result.flight_time;
        stats.total_height += result.max_height;
        stats.total_cubes += result.cube_count;

        // 최고 기록
        if (result.score > stats.best_score)
            stats.best_score = result.score;

        if (result.distance > stats.best_distance)
            stats.best_distance = result.distance;

        if (result.max_combo > stats.best_combo)
            stats.best_combo = result.max_combo;

        stats.updated_at = Timestamp.GetCurrentTimestamp();
    }
}
