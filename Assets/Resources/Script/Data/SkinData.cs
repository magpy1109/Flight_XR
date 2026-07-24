using Firebase.Firestore;

[FirestoreData]
public class SkinData
{
    [FirestoreProperty]
    public string skin_name { get; set; }

    [FirestoreProperty]
    public string rarity { get; set; }

    [FirestoreProperty]
    public int price { get; set; }

    [FirestoreProperty]
    public string unlock_type { get; set; }

    [FirestoreProperty]
    public string description { get; set; }
}