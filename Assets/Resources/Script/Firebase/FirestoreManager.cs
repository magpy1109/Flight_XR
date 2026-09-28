using System.Collections;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;

public class FirestoreManager : MonoBehaviour
{
    public static FirestoreManager Instance;

    private FirebaseFirestore db;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator Start()
    {
        // Firebase 초기화 완료 대기
        while (
            FirebaseManager.Instance == null ||
            !FirebaseManager.Instance.IsInitialized)
        {
            yield return null;
        }

        Debug.Log("FirestoreManager : Firebase 초기화 확인");

        db = FirebaseFirestore.DefaultInstance;
    }

    public void OnLoginCompleted()
    {
        Debug.Log("FirestoreManager : 로그인 확인");

        if (db == null)
        {
            db = FirebaseFirestore.DefaultInstance;
        }

        CreateUserIfNeeded();
    }

    private void CreateUserIfNeeded()
    {
        FirebaseUser user =
            FirebaseAuth.DefaultInstance.CurrentUser;

        if (user == null)
        {
            Debug.LogError("로그인이 되어있지 않습니다.");
            return;
        }

        Debug.Log("유저 확인 : " + user.UserId);

        DocumentReference doc =
            db.Collection("users").Document(user.UserId);

        doc.GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError(
                        "유저 확인 실패 : " +
                        task.Exception
                    );
                    return;
                }

                if (task.Result.Exists)
                {
                    Debug.Log("이미 존재하는 유저");
                    return;
                }

                Debug.Log("새 유저 생성");

                CreateUserDocument(user);
            });
    }

    private void CreateUserDocument(FirebaseUser user)
    {
        UserData data = new UserData();

        data.email = user.Email ?? "";
        data.photo_url =
            user.PhotoUrl?.ToString() ?? "";

        db.Collection("users")
            .Document(user.UserId)
            .SetAsync(data)
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompletedSuccessfully)
                {
                    Debug.LogError(
                        "유저 생성 실패 : " +
                        task.Exception
                    );
                    return;
                }

                Debug.Log("유저 생성 완료");

                CreateUserStats(user.UserId);
                GiveDefaultSkin(user.UserId);
            });
    }

    private void CreateUserStats(string userId)
    {
        UserStats stats = new UserStats();

        db.Collection("user_stats")
            .Document(userId)
            .SetAsync(stats)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log("기본 스탯 생성 완료");
                }
                else
                {
                    Debug.LogError(
                        "기본 스탯 생성 실패 : " +
                        task.Exception
                    );
                }
            });
    }

    private void GiveDefaultSkin(string userId)
    {
        UserSkin skin = new UserSkin();

        skin.user_id = userId;
        skin.skin_id = "default";

        db.Collection("user_skins")
            .Document(userId + "_default")
            .SetAsync(skin)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log("기본 스킨 지급 완료");
                }
                else
                {
                    Debug.LogError(
                        "기본 스킨 지급 실패 : " +
                        task.Exception
                    );
                }
            });
    }
}