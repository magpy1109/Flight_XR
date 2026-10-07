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

    /// <summary>Firebase 데이터 로드가 끝났을 때 (스킨씬 버튼 상태 갱신 등)</summary>
    public static event System.Action Loaded;

    // ---- 로컬 캐시 (Firebase 응답 전에도 스킨 화면이 바로 동작하도록) ----
    private const string KEY_OWNED_CACHE = "Cache_OwnedSkins";
    private const int MaxMissingRetry = 10;          // 새 유저 문서가 만들어지는 중일 때 재시도 횟수
    private const float MissingRetryDelay = 1f;

    // 로드 전에 누른 장착 (로드가 끝나면 Firebase에 저장)
    private string pendingSkinId;
    private string pendingTrailId;

    private int userRetry;
    private int statsRetry;

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
                    // 첫 실행 : FirestoreManager가 유저 문서를 만드는 중일 수 있음 → 잠시 후 다시 시도
                    if (userRetry++ < MaxMissingRetry)
                    {
                        Debug.LogWarning("users 데이터 없음 → 잠시 후 다시 시도");
                        StartCoroutine(RetryAfter(MissingRetryDelay, LoadUser));
                        return;
                    }

                    Debug.LogError("users 데이터 없음 → 기본값으로 진행");
                    CurrentUser = new UserData();
                    LoadStats(user.UserId);
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
                    if (statsRetry++ < MaxMissingRetry)
                    {
                        Debug.LogWarning("Stats 데이터 없음 → 잠시 후 다시 시도");
                        StartCoroutine(RetryAfter(MissingRetryDelay, () => LoadStats(userId)));
                        return;
                    }

                    Debug.LogError("Stats 데이터 없음 → 기본값으로 진행");
                    CurrentStats = new UserStats();
                    LoadOwnedSkins(userId);
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

                // 닉네임이 아직 없으면 겹치지 않는 닉네임을 자동으로 만들어 저장한 뒤 로드 완료
                EnsureNickname(FinishLoading);
            });
    }

    private void FinishLoading()
    {
        if (IsLoaded)
            return;

        IsLoaded = true;

        SaveOwnedCache();
        FlushPendingEquip();

        Debug.Log(
            "===== 게임 데이터 로드 완료 ====="
        );

        Loaded?.Invoke();
    }

    // =========================
    // 스킨 관련
    // =========================

    // ★ 임시 : 스킨 적용 테스트용으로 모든 스킨 / 트레일 잠금 해제.
    //   테스트가 끝나면 false로 바꾸면 원래대로(조건 달성 시 해금) 돌아간다.
    public static bool UnlockAllForTesting = true;

    public bool HasSkin(string skinId)
    {
        // 기본 스킨은 항상 보유
        // 기본 스킨 / 스킨별 기본 트레일("default"), 클래식 이펙트 트레일은 항상 보유
        if (skinId == "default" || skinId == TrailCatalog.ClassicKey || UnlockAllForTesting)
            return true;

        // Firebase 로드 전에는 지난번에 저장해 둔 보유 목록 사용
        if (!IsLoaded)
            return HasSkinCached(skinId);

        return OwnedSkins.Any(
            skin => skin.skin_id == skinId
        );
    }

    /// <summary>SaveManager가 없거나 로드 전일 때도 쓸 수 있는 보유 확인 (로컬 캐시)</summary>
    public static bool HasSkinCached(string skinId)
    {
        // 기본 스킨 / 스킨별 기본 트레일("default"), 클래식 이펙트 트레일은 항상 보유
        if (skinId == "default" || skinId == TrailCatalog.ClassicKey || UnlockAllForTesting)
            return true;

        string cache = PlayerPrefs.GetString(KEY_OWNED_CACHE, "default");
        foreach (string id in cache.Split(','))
        {
            if (id == skinId)
                return true;
        }

        return false;
    }

    private void SaveOwnedCache()
    {
        var ids = new List<string> { "default" };
        foreach (UserSkin skin in OwnedSkins)
        {
            if (skin != null && !string.IsNullOrEmpty(skin.skin_id) && !ids.Contains(skin.skin_id))
                ids.Add(skin.skin_id);
        }

        PlayerPrefs.SetString(KEY_OWNED_CACHE, string.Join(",", ids));
        PlayerPrefs.Save();
    }

    private IEnumerator RetryAfter(float seconds, System.Action action)
    {
        yield return new WaitForSecondsRealtime(seconds);
        action();
    }

    /// <summary>로드 전에 장착한 스킨 / 트레일을 Firebase에 저장</summary>
    private void FlushPendingEquip()
    {
        if (!string.IsNullOrEmpty(pendingSkinId))
        {
            string id = pendingSkinId;
            pendingSkinId = null;
            EquipSkin(id);
        }

        if (!string.IsNullOrEmpty(pendingTrailId))
        {
            string id = pendingTrailId;
            pendingTrailId = null;
            EquipTrail(id);
        }
    }

    private bool CanWriteUser =>
        IsLoaded && db != null && CurrentUser != null &&
        FirebaseAuth.DefaultInstance.CurrentUser != null;

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

        // 화면 / 게임에는 바로 적용 (로컬 저장)
        PlayerPrefs.SetInt(PlaneSkinState.PlaneKey, PlaneSkinState.ParseId(skinId, 0));
        PlayerPrefs.Save();

        if (CurrentUser != null)
            CurrentUser.equipped_skin_id = skinId;

        // Firebase 로드 전이면 로드가 끝난 뒤 저장
        if (!CanWriteUser)
        {
            pendingSkinId = skinId;
            return;
        }

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
    // 장착 트레일
    // =========================

    public void EquipTrail(string trailId)
    {
        if (!HasSkin(trailId))
        {
            Debug.LogWarning(
                "보유하지 않은 트레일입니다 : " + trailId
            );
            return;
        }

        PlayerPrefs.SetInt(PlaneSkinState.TrailKey, PlaneSkinState.ParseId(trailId, 0));
        PlayerPrefs.Save();

        if (CurrentUser != null)
            CurrentUser.equipped_trail_id = trailId;

        if (!CanWriteUser)
        {
            pendingTrailId = trailId;
            return;
        }

        string userId =
            FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        db.Collection("users")
            .Document(userId)
            .UpdateAsync(
                "equipped_trail_id",
                trailId
            )
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log(
                        "트레일 장착 저장 완료 : " + trailId
                    );
                }
                else
                {
                    Debug.LogError(
                        "트레일 장착 저장 실패 : " +
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
                SaveOwnedCache();

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

    // 자동 닉네임 : 겹치는지 확인하는 횟수 / 확인을 기다리는 최대 시간(초)
    private const int NicknameMaxTries = 8;
    private const float NicknameCheckTimeout = 6f;

    /// <summary>
    /// 닉네임이 아직 없으면(처음 실행, 또는 예전 기본값 "Player") 자동으로 만들어 서버에 저장하고 고정한다.
    /// - 다른 사람이 이미 쓰는 닉네임이면 다시 만든다. (users 에서 같은 닉네임을 찾아 확인)
    /// - 게임 안에서 닉네임을 바꾸는 기능은 없다. (한 번 정해지면 그대로)
    /// - 어떤 경우에도 done 은 한 번 호출된다. (확인이 실패하거나 늦어도 게임은 진행)
    /// </summary>
    private void EnsureNickname(System.Action done)
    {
        string current = CurrentUser != null && CurrentUser.nickname != null ? CurrentUser.nickname.Trim() : "";

        bool needsNickname = CurrentUser != null &&
                             (current.Length == 0 || current == "Player" || current == "Unknown" || current == "Nickname");

        if (!needsNickname)
        {
            done?.Invoke();
            return;
        }

        StartCoroutine(CreateUniqueNickname(done));
    }

    private IEnumerator CreateUniqueNickname(System.Action done)
    {
        string nickname = null;

        for (int attempt = 0; attempt < NicknameMaxTries; attempt++)
        {
            // 뒤로 갈수록 숫자 자릿수를 늘려 겹칠 가능성을 줄임
            string candidate = NicknameGenerator.Create(attempt < 3 ? 2 : 4);

            bool? taken = null;   // null = 아직 모름

            try
            {
                db.Collection("users")
                    .WhereEqualTo("nickname", candidate)
                    .Limit(1)
                    .GetSnapshotAsync()
                    .ContinueWithOnMainThread(task =>
                    {
                        if (task.IsCanceled || task.IsFaulted)
                        {
                            Debug.LogWarning("닉네임 중복 확인 실패 : " + task.Exception);
                            taken = false;   // 확인할 수 없으면 그대로 사용
                            return;
                        }

                        taken = task.Result.Count > 0;
                    });
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("닉네임 중복 확인 실패 : " + e.Message);
                taken = false;
            }

            float waited = 0f;
            while (taken == null && waited < NicknameCheckTimeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (taken == null)
            {
                Debug.LogWarning("닉네임 중복 확인 응답이 없어 그대로 사용 : " + candidate);
                nickname = candidate;
                break;
            }

            if (taken == false)
            {
                nickname = candidate;
                break;
            }

            Debug.Log("이미 사용 중인 닉네임 → 다시 생성 : " + candidate);
        }

        // 계속 겹치면 계정 ID 일부를 붙여 반드시 다르게
        if (nickname == null)
        {
            FirebaseUser user = FirebaseManager.Ready ? FirebaseAuth.DefaultInstance.CurrentUser : null;
            string suffix = user != null && user.UserId.Length >= 4 ? user.UserId.Substring(0, 4) : "0000";
            nickname = NicknameGenerator.Create(2) + suffix;
        }

        Debug.Log("닉네임 자동 생성 : " + nickname);

        ApplyProfileNickname(nickname);

        done?.Invoke();
    }

    /// <summary>
    /// 닉네임을 바꾸고 서버(users.nickname)에 저장. (자동 생성 닉네임 / Meta 프로필 이름 적용용)
    /// 이미 같은 이름이면 아무것도 하지 않고 false.
    /// </summary>
    public bool ApplyProfileNickname(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName) || CurrentUser == null)
            return false;

        profileName = profileName.Trim();

        if (CurrentUser.nickname == profileName)
            return false;

        CurrentUser.nickname = profileName;

        FirebaseUser user = FirebaseManager.Ready ? FirebaseAuth.DefaultInstance.CurrentUser : null;

        if (user == null || db == null)
        {
            Debug.LogWarning("닉네임을 서버에 저장하지 못했습니다 : 로그인 전");
            return true;
        }

        // 문서가 아직 없어도 저장되도록 병합 저장
        db.Collection("users")
            .Document(user.UserId)
            .SetAsync(
                new System.Collections.Generic.Dictionary<string, object> { { "nickname", profileName } },
                SetOptions.MergeAll)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("닉네임 저장 실패 : " + task.Exception);
                    return;
                }

                Debug.Log("닉네임 저장 완료 : " + profileName);
            });

        return true;
    }
}

/// <summary>
/// 자동 닉네임 만들기 : "꾸미는 말 + 이름 + 두 자리 숫자" (예 : 날쌘제비27)
/// 메인 메뉴 닉네임 칸에 들어가도록 짧게 만든다. 겹치는지는 SaveManager가 서버에서 확인한다.
/// </summary>
public static class NicknameGenerator
{
    private static readonly string[] Adjectives =
    {
        "빠른", "날쌘", "용감한", "푸른", "높은", "가벼운", "씩씩한", "멋진",
        "신난", "느긋한", "조용한", "당찬", "힘찬", "하얀", "작은", "큰"
    };

    private static readonly string[] Nouns =
    {
        "제비", "참새", "까치", "솔개", "독수리", "갈매기", "기러기", "두루미",
        "파랑새", "종이학", "구름", "바람", "비행기", "날개", "혜성", "별"
    };

    private static readonly System.Random random = new System.Random();

    /// <summary>digits : 뒤에 붙는 숫자 자릿수 (2 → 10~99, 4 → 1000~9999)</summary>
    public static string Create(int digits = 2)
    {
        int min = digits >= 4 ? 1000 : 10;
        int max = digits >= 4 ? 10000 : 100;

        return Adjectives[random.Next(Adjectives.Length)]
               + Nouns[random.Next(Nouns.Length)]
               + random.Next(min, max);
    }
}
