using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;

public class FirestoreManager : MonoBehaviour
{
    public static FirestoreManager Instance;

    private FirebaseFirestore db;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        CreateUserIfNeeded();
    }

    void CreateUserIfNeeded()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;

        if (user == null)
        {
            Debug.LogError("로그인이 되어있지 않습니다.");
            return;
        }

        DocumentReference doc =
            db.Collection("users").Document(user.UserId);

        doc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result.Exists)
            {
                Debug.Log("이미 존재하는 유저");
            }
            else
            {
                UserData data = new UserData();

                doc.SetAsync(data);

                Debug.Log("새 유저 생성 완료");
            }
        });
    }
}