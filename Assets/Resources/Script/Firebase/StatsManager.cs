using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance;

    private FirebaseFirestore db;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
    }

    public void UpdateStats(GameResult result)
    {
        DocumentReference doc =
            db.Collection("user_stats")
            .Document(result.user_id);

        doc.GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (!task.Result.Exists)
                {
                    Debug.LogError("user_stats 없음");
                    return;
                }

                UserStats stats =
                    task.Result.ConvertTo<UserStats>();

                stats.play_count++;

                stats.total_score += result.score;

                stats.total_distance += result.distance;

                if (result.score > stats.best_score)
                    stats.best_score = result.score;

                if (result.distance > stats.best_distance)
                    stats.best_distance = result.distance;

                doc.SetAsync(stats);

                Debug.Log("스탯 업데이트 완료");
            });
    }
}