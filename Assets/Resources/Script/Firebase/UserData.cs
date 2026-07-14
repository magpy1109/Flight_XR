using Firebase.Firestore;

[FirestoreData]
public class UserData
{
    [FirestoreProperty]
    public string nickname { get; set; } = "Player";

    [FirestoreProperty]
    public string email { get; set; } = "";

    [FirestoreProperty]
    public string photo_url { get; set; } = "";

    [FirestoreProperty]
    public string equipped_skin_id { get; set; } = "default";

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
    public Timestamp created_at { get; set; } = Timestamp.GetCurrentTimestamp();
}