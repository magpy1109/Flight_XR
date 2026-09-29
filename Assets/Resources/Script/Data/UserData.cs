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

    // 장착한 트레일 (스킨씬 트레일 탭). 예전 문서에 없으면 "default"
    [FirestoreProperty]
    public string equipped_trail_id { get; set; } = "default";

    [FirestoreProperty]
    public Timestamp created_at { get; set; }
        = Timestamp.GetCurrentTimestamp();
}