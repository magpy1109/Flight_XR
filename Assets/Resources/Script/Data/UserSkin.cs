using Firebase.Firestore;

[FirestoreData]
public class UserSkin
{
    [FirestoreProperty]
    public string user_id { get; set; }

    [FirestoreProperty]
    public string skin_id { get; set; } = "default";

    [FirestoreProperty]
    public Timestamp unlocked_at { get; set; }
        = Timestamp.GetCurrentTimestamp();
}