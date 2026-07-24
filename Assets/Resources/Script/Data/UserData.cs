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
    public Timestamp created_at { get; set; }
        = Timestamp.GetCurrentTimestamp();
}