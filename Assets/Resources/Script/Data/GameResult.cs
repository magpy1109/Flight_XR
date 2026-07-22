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

    [FirestoreProperty]
    public int ring_count { get; set; }

    [FirestoreProperty]
    public Timestamp created_at { get; set; } = Timestamp.GetCurrentTimestamp();
}