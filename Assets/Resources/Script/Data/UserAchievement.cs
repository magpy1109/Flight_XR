using Firebase.Firestore;

[FirestoreData]
public class UserAchievement
{
    [FirestoreProperty]
    public int progress { get; set; }

    [FirestoreProperty]
    public bool is_completed { get; set; }

    [FirestoreProperty]
    public Timestamp completed_at { get; set; }
}