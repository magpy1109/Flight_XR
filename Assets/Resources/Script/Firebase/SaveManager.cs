using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;
using System.Linq;
using System.Collections;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private FirebaseFirestore db;

    public UserData CurrentUser { get; private set; }

    public bool IsLoaded { get; private set; }

    public UserStats CurrentStats { get; private set; }

    public List<UserSkin> OwnedSkins { get; private set; }
        = new List<UserSkin>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        while (FirebaseAuth.DefaultInstance.CurrentUser == null)
        {
            yield return null;
        }

        Debug.Log("SaveManager : 로그인 확인");

        LoadGameData();
    }

    public void LoadGameData()
    {
        Debug.Log("===== 게임 데이터 불러오기 =====");

        LoadUser();
    }

    private void LoadUser()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;

        if (user == null)
        {
            Debug.LogError("로그인이 되어있지 않습니다.");
            return;
        }

        db.Collection("users")
        .Document(user.UserId)
        .GetSnapshotAsync()
        .ContinueWithOnMainThread(task =>
        {
            if (!task.Result.Exists)
            {
                Debug.LogError("users 데이터 없음");
                return;
            }

            CurrentUser = task.Result.ConvertTo<UserData>();

            Debug.Log("UserData 로드 완료");

            LoadStats();
        });
    }

    private void LoadStats()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;

        db.Collection("user_stats")
        .Document(user.UserId)
        .GetSnapshotAsync()
        .ContinueWithOnMainThread(task =>
        {
            if (!task.Result.Exists)
            {
                Debug.LogError("Stats 없음");
                return;
            }

            CurrentStats = task.Result.ConvertTo<UserStats>();

            Debug.Log("UserStats 로드 완료");

            LoadOwnedSkins();
        });
    }

    private void LoadOwnedSkins()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;

        db.Collection("user_skins")
        .WhereEqualTo("user_id", user.UserId)
        .GetSnapshotAsync()
        .ContinueWithOnMainThread(task =>
        {
            OwnedSkins.Clear();

            foreach (DocumentSnapshot doc in task.Result.Documents)
            {
                OwnedSkins.Add(doc.ConvertTo<UserSkin>());
            }

            Debug.Log($"스킨 {OwnedSkins.Count}개 로드 완료");

            IsLoaded = true;

            Debug.Log("===== 게임 데이터 로드 완료 =====");
        });
    }
}