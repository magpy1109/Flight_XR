using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Auth;

/// <summary>리더보드 종류</summary>
public enum LeaderboardMode
{
    Distance,   // 최고 거리 (user_stats.best_distance)
    Score       // 최고 점수 (user_stats.best_score, 노란 네모 점수)
}

/// <summary>
/// 리더보드 데이터 (Firestore user_stats).
///
/// - 거리 랭킹 / 점수 랭킹을 따로 불러오고, 한 번 불러온 랭킹은 씬에 있는 동안 기억한다. (탭 전환 시 바로 표시)
/// - 기록이 0인 사람(아직 플레이 안 함)은 랭킹에서 제외
/// - 닉네임은 한 번 불러오면 두 랭킹이 함께 사용
/// - 내 순위 : 나보다 기록이 높은 사람 수만 세서 계산 (전체 문서를 내려받지 않음)
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance;

    [Tooltip("랭킹에 보여줄 인원")]
    [SerializeField] private int topCount = 20;

    private FirebaseFirestore db;

    /// <summary>지금 선택된 랭킹의 목록 (예전 코드 호환용)</summary>
    public List<LeaderboardEntry> leaderboardEntries =
        new List<LeaderboardEntry>();

    public LeaderboardMode CurrentMode { get; private set; } = LeaderboardMode.Distance;

    /// <summary>지금 선택된 랭킹을 다 불러왔는지</summary>
    public bool IsLoaded => IsModeLoaded(CurrentMode);

    private readonly Dictionary<LeaderboardMode, List<LeaderboardEntry>> cache =
        new Dictionary<LeaderboardMode, List<LeaderboardEntry>>();

    private readonly HashSet<LeaderboardMode> loading = new HashSet<LeaderboardMode>();

    private readonly Dictionary<string, string> nicknames = new Dictionary<string, string>();

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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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

        // 마지막으로 보던 랭킹 먼저, 다른 랭킹도 미리 불러 둠
        LoadLeaderboard(LeaderboardModeSetting.Saved);
        LoadLeaderboard(LeaderboardModeSetting.Saved == LeaderboardMode.Distance
            ? LeaderboardMode.Score
            : LeaderboardMode.Distance);
    }

    // ---------- 조회 ----------

    public bool IsModeLoaded(LeaderboardMode mode)
    {
        return cache.ContainsKey(mode);
    }

    public List<LeaderboardEntry> GetEntries(LeaderboardMode mode)
    {
        List<LeaderboardEntry> list;
        return cache.TryGetValue(mode, out list) ? list : new List<LeaderboardEntry>();
    }

    /// <summary>보여줄 랭킹 바꾸기 (아직 안 불러왔으면 불러오기 시작)</summary>
    public void SetMode(LeaderboardMode mode)
    {
        CurrentMode = mode;
        leaderboardEntries = GetEntries(mode);

        if (!IsModeLoaded(mode))
            LoadLeaderboard(mode);
    }

    /// <summary>예전 코드 호환 : 지금 랭킹 다시 불러오기</summary>
    public void LoadLeaderboard()
    {
        cache.Remove(CurrentMode);
        LoadLeaderboard(CurrentMode);
    }

    public void LoadLeaderboard(LeaderboardMode mode)
    {
        if (db == null)
        {
            Debug.LogError(
                "LeaderboardManager : Firestore가 초기화되지 않았습니다."
            );
            return;
        }

        if (loading.Contains(mode))
            return;

        loading.Add(mode);

        string field = LeaderboardQueries.FieldOf(mode);

        Debug.Log($"===== 리더보드 불러오기 시작 ({mode}) =====");

        db.Collection("user_stats")
            .WhereGreaterThan(field, 0)
            .OrderByDescending(field)
            .Limit(topCount)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError(
                        $"리더보드 조회 실패 ({mode}) : " + task.Exception
                    );

                    Finish(mode, new List<LeaderboardEntry>());
                    return;
                }

                var entries = new List<LeaderboardEntry>();
                int rank = 0;
                float previous = float.NaN;
                int index = 0;

                foreach (DocumentSnapshot doc in task.Result.Documents)
                {
                    UserStats stats = doc.ConvertTo<UserStats>();

                    LeaderboardEntry entry = new LeaderboardEntry();
                    entry.userId = doc.Id;
                    entry.bestDistance = stats.best_distance;
                    entry.bestScore = stats.best_score;

                    // 같은 기록은 같은 순위
                    float value = entry.Value(mode);
                    index++;
                    if (value != previous)
                        rank = index;
                    previous = value;
                    entry.rank = rank;

                    string nickname;
                    entry.nickname = nicknames.TryGetValue(doc.Id, out nickname)
                        ? nickname
                        : "불러오는 중...";

                    entries.Add(entry);
                }

                Debug.Log($"리더보드 ({mode}) {entries.Count}명 조회 완료");

                LoadNicknames(mode, entries);
            });
    }

    private void LoadNicknames(LeaderboardMode mode, List<LeaderboardEntry> entries)
    {
        var missing = new List<LeaderboardEntry>();
        foreach (LeaderboardEntry entry in entries)
        {
            if (!nicknames.ContainsKey(entry.userId))
                missing.Add(entry);
        }

        if (missing.Count == 0)
        {
            Finish(mode, entries);
            return;
        }

        int pending = missing.Count;

        foreach (LeaderboardEntry entry in missing)
        {
            LeaderboardEntry target = entry;

            db.Collection("users")
                .Document(target.userId)
                .GetSnapshotAsync()
                .ContinueWithOnMainThread(task =>
                {
                    string nickname = "Player";

                    if (task.IsCanceled || task.IsFaulted)
                    {
                        Debug.LogError("닉네임 조회 실패 : " + task.Exception);
                        nickname = "Unknown";
                    }
                    else if (task.Result.Exists)
                    {
                        UserData user = task.Result.ConvertTo<UserData>();
                        if (!string.IsNullOrEmpty(user.nickname))
                            nickname = user.nickname;
                    }
                    else
                    {
                        nickname = "Unknown";
                    }

                    nicknames[target.userId] = nickname;

                    pending--;
                    if (pending <= 0)
                    {
                        foreach (LeaderboardEntry e in entries)
                        {
                            string n;
                            if (nicknames.TryGetValue(e.userId, out n))
                                e.nickname = n;
                        }

                        Finish(mode, entries);
                    }
                });
        }
    }

    private void Finish(LeaderboardMode mode, List<LeaderboardEntry> entries)
    {
        loading.Remove(mode);
        cache[mode] = entries;

        if (mode == CurrentMode)
            leaderboardEntries = entries;

        Debug.Log($"===== 리더보드 ({mode}) 전체 데이터 로드 완료 =====");
    }

    // ---------- 내 순위 ----------

    /// <summary>예전 코드 호환 : 거리 기준 내 순위</summary>
    public void LoadMyRank(System.Action<int, float> callback)
    {
        LoadMyRank(LeaderboardMode.Distance, callback);
    }

    /// <summary>내 최고 기록 (거리 m / 점수). 서버 기록을 못 불러왔으면 기기에 저장된 최고 거리</summary>
    public float MyBest(LeaderboardMode mode)
    {
        UserStats stats = SaveManager.Instance != null ? SaveManager.Instance.CurrentStats : null;

        if (stats != null)
            return mode == LeaderboardMode.Score ? stats.best_score : stats.best_distance;

        return mode == LeaderboardMode.Score ? 0f : PlayerPrefs.GetFloat("Local_BestDistance", 0f);
    }

    /// <summary>내 순위 (기록이 없으면 rank = -1). 어떤 경우에도 callback은 한 번 호출된다.</summary>
    public void LoadMyRank(LeaderboardMode mode, System.Action<int, float> callback)
    {
        float myValue = MyBest(mode);

        // 랭킹 목록 안에 내가 있으면 목록 순위를 그대로 사용 (목록과 내 기록 줄이 항상 같게)
        FirebaseUser user = FirebaseManager.Ready ? FirebaseAuth.DefaultInstance.CurrentUser : null;
        if (user != null)
        {
            foreach (LeaderboardEntry entry in GetEntries(mode))
            {
                if (entry.userId == user.UserId)
                {
                    callback?.Invoke(entry.rank, Mathf.Max(myValue, entry.Value(mode)));
                    return;
                }
            }
        }

        LeaderboardQueries.GetRank(mode, myValue, rank =>
        {
            Debug.Log($"내 순위 ({mode}) : {rank}위 / 기록 : {myValue}");
            callback?.Invoke(rank, myValue);
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
    public int bestScore;

    public float Value(LeaderboardMode mode)
    {
        return mode == LeaderboardMode.Score ? bestScore : bestDistance;
    }

    /// <summary>화면에 보여줄 기록 ("1,234.5m" / "1,230점")</summary>
    public string ValueText(LeaderboardMode mode)
    {
        return LeaderboardQueries.Format(mode, Value(mode));
    }
}

/// <summary>마지막으로 본 리더보드 종류 (PlayerPrefs)</summary>
public static class LeaderboardModeSetting
{
    private const string Key = "Leaderboard_Mode";

    public static LeaderboardMode Saved
    {
        get { return PlayerPrefs.GetInt(Key, 0) == 1 ? LeaderboardMode.Score : LeaderboardMode.Distance; }
        set
        {
            PlayerPrefs.SetInt(Key, value == LeaderboardMode.Score ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}

/// <summary>리더보드 공용 조회 (리더보드 씬 / 메인 메뉴 내 순위)</summary>
public static class LeaderboardQueries
{
    public static string FieldOf(LeaderboardMode mode)
    {
        return mode == LeaderboardMode.Score ? "best_score" : "best_distance";
    }

    public static string Format(LeaderboardMode mode, float value)
    {
        return mode == LeaderboardMode.Score
            ? Mathf.RoundToInt(value).ToString("N0") + "점"
            : value.ToString("N1") + "m";
    }

    /// <summary>
    /// 내 순위 = 나보다 기록이 높은 사람 수 + 1 (같은 기록은 같은 순위).
    /// 기록이 0이면(아직 플레이 안 함) -1. 어떤 경우에도 callback은 한 번 호출된다.
    ///
    /// 개수 세기(집계) 조회를 먼저 쓰고, 지원되지 않거나 실패하면 해당 문서를 받아서 센다.
    /// (예전에는 집계 조회가 실패하면 callback이 불리지 않아 내 기록 줄이 갱신되지 않았음)
    /// </summary>
    public static void GetRank(LeaderboardMode mode, float myValue, System.Action<int> callback)
    {
        bool done = false;
        System.Action<int> finish = rank =>
        {
            if (done)
                return;
            done = true;
            callback?.Invoke(rank);
        };

        if (myValue <= 0f || !FirebaseManager.Ready)
        {
            finish(-1);
            return;
        }

        Query query;
        try
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            string field = FieldOf(mode);

            query = mode == LeaderboardMode.Score
                ? db.Collection("user_stats").WhereGreaterThan(field, Mathf.RoundToInt(myValue))
                : db.Collection("user_stats").WhereGreaterThan(field, myValue);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"내 순위 조회 준비 실패 ({mode}) : " + e);
            finish(-1);
            return;
        }

        try
        {
            query.Count
                .GetSnapshotAsync(AggregateSource.Server)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        Debug.LogWarning($"내 순위 개수 조회 실패 → 문서로 다시 계산 ({mode}) : " + task.Exception);
                        CountByDocuments(mode, query, finish);
                        return;
                    }

                    finish((int)task.Result.Count + 1);
                });
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"개수 조회를 쓸 수 없음 → 문서로 계산 ({mode}) : " + e.Message);
            CountByDocuments(mode, query, finish);
        }
    }

    private static void CountByDocuments(LeaderboardMode mode, Query query, System.Action<int> finish)
    {
        try
        {
            query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError($"내 순위 조회 실패 ({mode}) : " + task.Exception);
                    finish(-1);
                    return;
                }

                finish(task.Result.Count + 1);
            });
        }
        catch (System.Exception e)
        {
            Debug.LogError($"내 순위 조회 실패 ({mode}) : " + e);
            finish(-1);
        }
    }
}
