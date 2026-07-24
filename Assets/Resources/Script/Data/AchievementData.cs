using Firebase.Firestore;

[FirestoreData]
public class AchievementData
{
    [FirestoreProperty]
    public string achievement_name { get; set; }

    [FirestoreProperty]
    public string description { get; set; }

    [FirestoreProperty]
    public string condition_type { get; set; }

    [FirestoreProperty]
    public int condition_value { get; set; }

    [FirestoreProperty]
    public string reward_type { get; set; }

    [FirestoreProperty]
    public int reward_value { get; set; }
}