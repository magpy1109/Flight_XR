using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Auth;

public class SkinManager : MonoBehaviour
{
    public static SkinManager Instance;

    private FirebaseFirestore db;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
    }

    public void UnlockSkin(string skinId)
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;

        if (user == null)
        {
            Debug.LogError("로그인 되어있지 않습니다.");
            return;
        }

        string documentId = user.UserId + "_" + skinId;

        DocumentReference doc =
            db.Collection("user_skins")
              .Document(documentId);

        doc.GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError(task.Exception);
                    return;
                }

                if (task.Result.Exists)
                {
                    Debug.Log("이미 보유한 스킨");
                    return;
                }

                CreateSkin(user.UserId, skinId);
            });
    }

    private void CreateSkin(string userId, string skinId)
    {
        UserSkin skin = new UserSkin();

        skin.user_id = userId;
        skin.skin_id = skinId;

        db.Collection("user_skins")
            .Document(userId + "_" + skinId)
            .SetAsync(skin)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log("새 스킨 획득 : " + skinId);
                }
                else
                {
                    Debug.LogError("스킨 지급 실패");
                }
            });
    }
}