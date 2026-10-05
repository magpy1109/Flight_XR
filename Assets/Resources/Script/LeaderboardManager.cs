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
/// - 기록은 받는 즉시 화면에 보여 주고(Updated), 닉네임은 불러오는 대로 채운다
/// - 문서 형식이 다르거나 조회가 실패해도 멈추지 않는다 (해당 문서만 건너뜀 / 전체 문서로 재시도)
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

    /// <summary>랭킹 목록이 바뀜 (기록 로드 완료 / 닉네임 로드 완료)</summary>
    public event System.Action<LeaderboardMode> Updated;

    /// <summary>랭킹에 보여주는 인원</summary>
    public int TopCount => topCount;

    /// <summary>내 계정 ID (로그인 전이면 null)</summary>
    public static string MyUserId
    {
        get
        {
            try
            {
                if (!FirebaseManager.Ready)
                    return null;

                FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
                return user != null ? user.UserId : null;
            }
            catch (System.Exception)
            {
                return null;
            }
        }
    }

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

        try
        {
            db.Collection("user_stats")
                .WhereGreaterThan(field, 0)
                .OrderByDescending(field)
                .Limit(topCount)
                .GetSnapshotAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        Debug.LogWarning(
                            $"리더보드 조회 실패 → 전체 문서로 다시 시도 ({mode}) : " + task.Exception
                        );

                        LoadByAllDocuments(mode);
                        return;
                    }

                    OnDocuments(mode, task.Result.Documents);
                });
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"리더보드 조회를 시작하지 못함 → 전체 문서로 다시 시도 ({mode}) : " + e);
            LoadByAllDocuments(mode);
        }
    }

    /// <summary>정렬 조회가 실패했을 때 : 문서를 모두 받아 기기에서 정렬</summary>
    private void LoadByAllDocuments(LeaderboardMode mode)
    {
        try
        {
            db.Collection("user_stats")
                .GetSnapshotAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        Debug.LogError($"리더보드 조회 실패 ({mode}) : " + task.Exception);
                        Finish(mode, new List<LeaderboardEntry>());
                        return;
                    }

                    OnDocuments(mode, task.Result.Documents);
                });
        }
        catch (System.Exception e)
        {
            Debug.LogError($"리더보드 조회 실패 ({mode}) : " + e);
            Finish(mode, new List<LeaderboardEntry>());
        }
    }

    /// <summary>
    /// 받은 문서 → 랭킹 목록. 어떤 경우에도 Finish가 호출된다.
    /// (예전에는 문서 하나라도 형식이 달라 변환에 실패하면 여기서 멈춰 화면이 계속 "불러오는 중"이었음)
    /// </summary>
    private void OnDocuments(LeaderboardMode mode, IEnumerable<DocumentSnapshot> documents)
    {
        var entries = new List<LeaderboardEntry>();

        try
        {
            foreach (DocumentSnapshot doc in documents)
            {
                try
                {
                    Dictionary<string, object> data = doc.ToDictionary();

                    LeaderboardEntry entry = new LeaderboardEntry();
                    entry.userId = doc.Id;
                    entry.bestDistance = (float)Number(data, "best_distance");
                    entry.bestScore = (int)System.Math.Round(Number(data, "best_score"));

                    if (entry.Value(mode) <= 0f)
                        continue;   // 기록 없음

                    string nickname;
                    entry.nickname = nicknames.TryGetValue(doc.Id, out nickname)
                        ? nickname
                        : "불러오는 중...";

                    entries.Add(entry);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"리더보드 문서를 읽지 못해 건너뜀 ({doc.Id}) : " + e.Message);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"리더보드 문서 처리 실패 ({mode}) : " + e);
        }

        entries.Sort((a, b) => b.Value(mode).CompareTo(a.Value(mode)));
        if (entries.Count > topCount)
            entries.RemoveRange(topCount, entries.Count - topCount);

        AssignRanks(entries, mode);

        Debug.Log($"리더보드 ({mode}) {entries.Count}명 조회 완료");

        // 기록은 바로 보여 주고, 닉네임은 불러오는 대로 채움
        Finish(mode, entries);
        LoadNicknames(mode, entries);
    }

    /// <summary>숫자 필드 읽기 (정수 / 실수 어느 쪽으로 저장돼 있어도, 없으면 0)</summary>
    private static double Number(Dictionary<string, object> data, string field)
    {
        object value;
        if (data == null || !data.TryGetValue(field, out value) || value == null)
            return 0d;

        try
        {
            return System.Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (System.Exception)
        {
            return 0d;
        }
    }

    /// <summary>순위 매기기 (기록 높은 순으로 정렬된 목록, 같은 기록은 같은 순위)</summary>
    public static void AssignRanks(List<LeaderboardEntry> entries, LeaderboardMode mode)
    {
        int rank = 0;
        float previous = float.NaN;

        for (int i = 0; i < entries.Count; i++)
        {
            float value = entries[i].Value(mode);
            if (value != previous)
                rank = i + 1;
            previous = value;
            entries[i].rank = rank;
        }
    }

    private void LoadNicknames(LeaderboardMode mode, List<LeaderboardEntry> entries)
    {
        var missing = new List<string>();
        foreach (LeaderboardEntry entry in entries)
        {
            if (!nicknames.ContainsKey(entry.userId) && !missing.Contains(entry.userId))
                missing.Add(entry.userId);
        }

        if (missing.Count == 0)
            return;

        int pending = missing.Count;

        System.Action<string, string> done = (userId, nickname) =>
        {
            nicknames[userId] = nickname;

            foreach (LeaderboardEntry e in entries)
            {
                if (e.userId == userId)
                    e.nickname = nickname;
            }

            pending--;
            if (pending <= 0)
                Updated?.Invoke(mode);   // 화면 다시 그리기
        };

        foreach (string id in missing)
        {
            string userId = id;

            try
            {
                db.Collection("users")
                    .Document(userId)
                    .GetSnapshotAsync()
                    .ContinueWithOnMainThread(task =>
                    {
                        string nickname = "Unknown";

                        try
                        {
                            if (task.IsCanceled || task.IsFaulted)
                            {
                                Debug.LogWarning("닉네임 조회 실패 : " + task.Exception);
                            }
                            else if (task.Result.Exists)
                            {
                                nickname = "Player";

                                string value;
                                if (task.Result.TryGetValue("nickname", out value) && !string.IsNullOrEmpty(value))
                                    nickname = value;
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning("닉네임을 읽지 못함 : " + e.Message);
                        }

                        done(userId, nickname);
                    });
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("닉네임 조회 실패 : " + e.Message);
                done(userId, "Unknown");
            }
        }
    }

    private void Finish(LeaderboardMode mode, List<LeaderboardEntry> entries)
    {
        loading.Remove(mode);
        cache[mode] = entries;

        if (mode == CurrentMode)
            leaderboardEntries = entries;

        Debug.Log($"===== 리더보드 ({mode}) 기록 로드 완료 =====");

        Updated?.Invoke(mode);
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
        string myId = MyUserId;
        if (myId != null)
        {
            foreach (LeaderboardEntry entry in GetEntries(mode))
            {
                if (entry.userId == myId)
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
