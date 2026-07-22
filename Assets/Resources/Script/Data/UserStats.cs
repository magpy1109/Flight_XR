using Firebase.Firestore;

[FirestoreData]
public class UserStats
{
    [FirestoreProperty]
    public int best_score { get; set; } = 0;

    [FirestoreProperty]
    public int total_score { get; set; } = 0;

    [FirestoreProperty]
    public int play_count { get; set; } = 0;

    [FirestoreProperty]
    public float best_distance { get; set; } = 0;

    [FirestoreProperty]
    public float total_distance { get; set; } = 0;

    [FirestoreProperty]
    public float total_play_time { get; set; } = 0;

    [FirestoreProperty]
    public float total_height { get; set; } = 0;

    [FirestoreProperty]
    public Timestamp updated_at { get; set; } =
        Timestamp.GetCurrentTimestamp();
}