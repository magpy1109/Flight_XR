using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Auth;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance;

    private FirebaseFirestore db;

    public List<LeaderboardEntry> leaderboardEntries =
        new List<LeaderboardEntry>();

    public bool IsLoaded { get; private set; }

    private int pendingNicknameCount;

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

    private System.Collections.IEnumerator Start()
    {
        // Firebase 초기화 대기
        while (
            FirebaseManager.Instance == null ||
            !FirebaseManager.Instance.IsInitialized)
        {
            yield return null;
        }

        Debug.Log("LeaderboardManager : Firebase 초기화 확인");

        db = FirebaseFirestore.DefaultInstance;

        LoadLeaderboard();
    }

    public void LoadLeaderboard()
    {
        IsLoaded = false;

        if (db == null)
        {
            Debug.LogError(
                "LeaderboardManager : Firestore가 초기화되지 않았습니다."
            );
            return;
        }

        Debug.Log("===== 리더보드 불러오기 시작 =====");

        db.Collection("user_stats")
            .OrderByDescending("best_distance")
            .Limit(10)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError(
                        "리더보드 조회 실패 : " + task.Exception
                    );

                    IsLoaded = true;
                    return;
                }

                leaderboardEntries.Clear();

                int rank = 1;

                foreach (DocumentSnapshot doc in task.Result.Documents)
                {
                    UserStats stats =
                        doc.ConvertTo<UserStats>();

                    LeaderboardEntry entry =
                        new LeaderboardEntry();

                    entry.rank = rank;
                    entry.userId = doc.Id;
                    entry.bestDistance = stats.best_distance;
                    entry.nickname = "불러오는 중...";

                    leaderboardEntries.Add(entry);

                    rank++;
                }

                Debug.Log(
                    $"리더보드 {leaderboardEntries.Count}명 조회 완료"
                );

                pendingNicknameCount =
                    leaderboardEntries.Count;

                if (pendingNicknameCount == 0)
                {
                    IsLoaded = true;
                    return;
                }

                foreach (LeaderboardEntry entry in leaderboardEntries)
                {
                    LoadNickname(entry);
                }
            });
    }

    private void LoadNickname(LeaderboardEntry entry)
    {
        db.Collection("users")
            .Document(entry.userId)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError(
                        "닉네임 조회 실패 : " + task.Exception
                    );

                    entry.nickname = "Unknown";
                }
                else if (!task.Result.Exists)
                {
                    entry.nickname = "Unknown";
                }
                else
                {
                    UserData user =
                        task.Result.ConvertTo<UserData>();

                    entry.nickname =
                        string.IsNullOrEmpty(user.nickname)
                        ? "Player"
                        : user.nickname;
                }

                Debug.Log(
                    $"{entry.rank}위 | {entry.nickname} | " +
                    $"{entry.bestDistance:F2}m"
                );

                pendingNicknameCount--;

                if (pendingNicknameCount <= 0)
                {
                    IsLoaded = true;

                    Debug.Log(
                        "===== 리더보드 전체 데이터 로드 완료 ====="
                    );
                }
            });
    }

    public void LoadMyRank(System.Action<int, float> callback)
    {
        FirebaseUser user =
            FirebaseAuth.DefaultInstance.CurrentUser;

        if (user == null)
        {
            Debug.LogError(
                "LeaderboardManager : 로그인된 유저가 없습니다."
            );

            callback?.Invoke(-1, 0f);
            return;
        }

        if (SaveManager.Instance == null ||
            SaveManager.Instance.CurrentStats == null)
        {
            Debug.LogError(
                "LeaderboardManager : 내 스탯을 불러오지 못했습니다."
            );

            callback?.Invoke(-1, 0f);
            return;
        }

        float myBestDistance =
            SaveManager.Instance.CurrentStats.best_distance;

        db.Collection("user_stats")
            .OrderByDescending("best_distance")
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError(
                        "내 순위 조회 실패 : " + task.Exception
                    );

                    callback?.Invoke(-1, myBestDistance);
                    return;
                }

                int rank = 1;

                foreach (DocumentSnapshot doc in task.Result.Documents)
                {
                    if (doc.Id == user.UserId)
                    {
                        Debug.Log(
                            $"내 순위 : {rank}위 / " +
                            $"최고 거리 : {myBestDistance:F1}m"
                        );

                        callback?.Invoke(
                            rank,
                            myBestDistance
                        );

                        return;
                    }

                    rank++;
                }

                Debug.LogWarning(
                    "리더보드에서 현재 유저를 찾지 못했습니다."
                );

                callback?.Invoke(
                    -1,
                    myBestDistance
                );
            });
    }
}

[System.Serializable]
public class LeaderboardEntry
{
    public int rank;
    public string userId;
    public string nickname;
    public float bestDistance;
}