using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private FirebaseFirestore db;

    public UserData CurrentUser { get; private set; }

    public UserStats CurrentStats { get; private set; }

    public List<UserSkin> OwnedSkins { get; private set; }
        = new List<UserSkin>();

    public bool IsLoaded { get; private set; }

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
        // Firebase 초기화 대기
        while (
            FirebaseManager.Instance == null ||
            !FirebaseManager.Instance.IsInitialized)
        {
            yield return null;
        }

        Debug.Log("SaveManager : Firebase 초기화 확인");

        db = FirebaseFirestore.DefaultInstance;

        // 로그인 대기
        while (FirebaseAuth.DefaultInstance.CurrentUser == null)
        {
            yield return null;
        }

        Debug.Log("SaveManager : 로그인 확인");

        LoadGameData();
    }

    public void LoadGameData()
    {
        if (IsLoaded)
        {
            Debug.Log("SaveManager : 이미 로드되어 있습니다.");
            return;
        }

        Debug.Log("===== 게임 데이터 불러오기 =====");

        LoadUser();
    }

    private void LoadUser()
    {
        FirebaseUser user =
            FirebaseAuth.DefaultInstance.CurrentUser;

        if (user == null)
        {
            Debug.LogError(
                "SaveManager : 로그인이 되어있지 않습니다."
            );
            return;
        }

        db.Collection("users")
            .Document(user.UserId)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError(
                        "UserData 로드 실패 : " +
                        task.Exception
                    );
                    return;
                }

                if (!task.Result.Exists)
                {
                    Debug.LogError("users 데이터 없음");
                    return;
                }

                CurrentUser =
                    task.Result.ConvertTo<UserData>();

                Debug.Log("UserData 로드 완료");

                LoadStats(user.UserId);
            });
    }

    private void LoadStats(string userId)
    {
        db.Collection("user_stats")
            .Document(userId)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError(
                        "UserStats 로드 실패 : " +
                        task.Exception
                    );
                    return;
                }

                if (!task.Result.Exists)
                {
                    Debug.LogError("Stats 데이터 없음");
                    return;
                }

                CurrentStats =
                    task.Result.ConvertTo<UserStats>();

                Debug.Log("UserStats 로드 완료");

                LoadOwnedSkins(userId);
            });
    }

    private void LoadOwnedSkins(string userId)
    {
        db.Collection("user_skins")
            .WhereEqualTo("user_id", userId)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError(
                        "OwnedSkins 로드 실패 : " +
                        task.Exception
                    );
                    return;
                }

                OwnedSkins.Clear();

                foreach (DocumentSnapshot doc in
                         task.Result.Documents)
                {
                    OwnedSkins.Add(
                        doc.ConvertTo<UserSkin>()
                    );
                }

                Debug.Log(
                    $"스킨 {OwnedSkins.Count}개 로드 완료"
                );

                IsLoaded = true;

                Debug.Log(
                    "===== 게임 데이터 로드 완료 ====="
                );
            });
    }

    // =========================
    // 스킨 관련
    // =========================

    public bool HasSkin(string skinId)
    {
        return OwnedSkins.Any(
            skin => skin.skin_id == skinId
        );
    }

    // =========================
    // 장착 스킨
    // =========================

    public void EquipSkin(string skinId)
    {
        if (!HasSkin(skinId))
        {
            Debug.LogWarning(
                "보유하지 않은 스킨입니다 : " + skinId
            );
            return;
        }

        CurrentUser.equipped_skin_id = skinId;

        string userId =
            FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        db.Collection("users")
            .Document(userId)
            .UpdateAsync(
                "equipped_skin_id",
                skinId
            )
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log(
                        "스킨 장착 저장 완료 : " + skinId
                    );
                }
                else
                {
                    Debug.LogError(
                        "스킨 장착 저장 실패 : " +
                        task.Exception
                    );
                }
            });
    }

    // =========================
    // 스킨 해제
    // =========================

    public void UnlockSkin(string skinId)
    {
        if (string.IsNullOrEmpty(skinId))
        {
            Debug.LogWarning("skinId가 비어 있습니다.");
            return;
        }

        if (HasSkin(skinId))
        {
            Debug.Log(
                "이미 보유한 스킨 : " + skinId
            );
            return;
        }

        FirebaseUser user =
            FirebaseAuth.DefaultInstance.CurrentUser;

        if (user == null)
        {
            Debug.LogError("로그인이 되어있지 않습니다.");
            return;
        }

        UserSkin skin = new UserSkin();

        skin.user_id = user.UserId;
        skin.skin_id = skinId;

        string documentId =
            user.UserId + "_" + skinId;

        db.Collection("user_skins")
            .Document(documentId)
            .SetAsync(skin)
            .ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompletedSuccessfully)
                {
                    Debug.LogError(
                        "스킨 해제 저장 실패 : " +
                        task.Exception
                    );
                    return;
                }

                OwnedSkins.Add(skin);

                Debug.Log(
                    "스킨 해제 완료 : " + skinId
                );
            });
    }

    public void UpdateLocalStats(UserStats stats)
    {
        CurrentStats = stats;

        Debug.Log("SaveManager : 로컬 스탯 갱신 완료");
    }

    public void UpdateNickname(string newNickname)
    {
        if (string.IsNullOrWhiteSpace(newNickname))
        {
            Debug.LogWarning("닉네임이 비어 있습니다.");
            return;
        }

        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;

        if (user == null)
        {
            Debug.LogError("로그인된 유저가 없습니다.");
            return;
        }

        newNickname = newNickname.Trim();

        CurrentUser.nickname = newNickname;

        string userId = user.UserId;

        db.Collection("users")
            .Document(userId)
            .UpdateAsync("nickname", newNickname)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError(
                        "닉네임 저장 실패 : " + task.Exception
                    );
                    return;
                }

                Debug.Log(
                    "닉네임 저장 완료 : " + newNickname
                );
            });
    }
}