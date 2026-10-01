using Firebase.Firestore;

[FirestoreData]
public class GameResult
{
    [FirestoreProperty]
    public string user_id { get; set; }

    [FirestoreProperty]
    public int score { get; set; }

    [FirestoreProperty]
    public float distance { get; set; }

    [FirestoreProperty]
    public int flight_time { get; set; }

    [FirestoreProperty]
    public float max_height { get; set; }

    // 예전 링 기능 (링은 폐기되어 항상 0, 예전 기록 호환용으로 남겨 둠)
    [FirestoreProperty]
    public int ring_count { get; set; }

    // 이번 판에 먹은 노란 네모 개수
    [FirestoreProperty]
    public int cube_count { get; set; }

    // 이번 판 최대 연속 개수
    [FirestoreProperty]
    public int max_combo { get; set; }

    [FirestoreProperty]
    public Timestamp created_at { get; set; } = Timestamp.GetCurrentTimestamp();
}