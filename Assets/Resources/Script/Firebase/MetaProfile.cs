using System.Collections;
using UnityEngine;
using Oculus.Platform;
using Oculus.Platform.Models;

/// <summary>
/// [현재 사용 안 함 : Enabled = false] 닉네임 = Meta(Quest) 계정 프로필 이름으로 고정.
///
/// - 앱을 켤 때 Meta Platform SDK로 로그인한 계정의 프로필 이름을 받아 DB(users.nickname)에 저장한다.
///   (Meta에서 이름을 바꾸면 다음 실행 때 따라 바뀜. 게임 안에서는 닉네임을 바꿀 수 없음)
/// - 프로필을 받지 못하면(Unity 에디터 실행, 대시보드 승인 전, 권한 없는 계정 등) 기존 닉네임을 그대로 둔다.
/// - 씬에 따로 배치하지 않아도 자동으로 실행된다.
///
/// 필요한 대시보드 설정 (developers.meta.com)
///   1) 앱 ID : 아래 AppId
///   2) Data Use Checkup 에서 "User ID", "User Profile" 사용 신청
///   3) 헤드셋 계정이 앱 개발 조직 멤버이거나 릴리스 채널에 초대돼 있어야 함
/// </summary>
public class MetaProfile : MonoBehaviour
{
    /// <summary>Meta 개발자 대시보드 > Development > API 의 App ID</summary>
    public const string AppId = "1361010413762943";

    /// <summary>
    /// Meta 프로필 조회 사용 여부. 지금은 꺼 둠 → 닉네임은 자동 생성(NicknameGenerator)으로 고정.
    ///
    /// 끈 이유 : 헤드셋 계정이 Meta에서 이 앱의 개발자로 인정되지 않아 초기화가 실패하고
    /// (DeveloperNotEntitled), 한국 계정은 릴리스 채널에도 참여할 수 없어 다른 계정에서는 쓸 수 없음.
    /// 나중에 권한 문제가 풀리면 true로 바꾸면 프로필 이름이 닉네임으로 들어간다.
    /// </summary>
    public static readonly bool Enabled = false;

    /// <summary>받아온 프로필 이름 (아직 못 받았으면 null)</summary>
    public static string DisplayName { get; private set; }

    /// <summary>프로필 조회가 끝났는지 (성공 / 실패 모두)</summary>
    public static bool IsDone { get; private set; }

    /// <summary>닉네임이 프로필 이름으로 바뀜 (화면 갱신용)</summary>
    public static event System.Action<string> NicknameChanged;

    // 응답을 기다리는 시간 (앱이 실제로 돌아간 시간 기준 : 헤드셋을 벗어 앱이 멈춰 있던 시간은 세지 않음)
    private const float RetryInterval = 15f;
    private const int MaxRequests = 3;

    private static MetaProfile instance;

    private bool waiting;
    private bool initialized;
    private float waited;
    private int requests;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        DisplayName = null;
        IsDone = false;
        NicknameChanged = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance != null)
            return;

        GameObject go = new GameObject("MetaProfile");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MetaProfile>();
    }

    private void Start()
    {
        if (!Enabled)
        {
            IsDone = true;
            return;
        }

        // 헤드셋(안드로이드)에서만 조회. 에디터에서는 Meta 계정 정보가 없어 건너뜀
        if (UnityEngine.Application.isEditor || UnityEngine.Application.platform != RuntimePlatform.Android)
        {
            Debug.Log("[MetaProfile] 헤드셋이 아니므로 Meta 프로필 조회를 건너뜁니다. (기존 닉네임 유지)");
            IsDone = true;
            return;
        }

        try
        {
            if (Core.IsInitialized())
            {
                Debug.Log("[MetaProfile] Platform 이미 초기화됨");
                initialized = true;
                RequestUser();
            }
            else
            {
                Debug.Log("[MetaProfile] Platform 초기화 시작 (App ID " + AppId + ")");

                // 바로 결과가 나오는 방식으로 초기화.
                // (AsyncInitialize는 초기화가 실패하면 아무 응답 없이 끝나서 원인을 알 수 없었음)
                var status = Core.Initialize(AppId);

                if (status == null || !status.IsSuccess())
                {
                    Debug.LogWarning("[MetaProfile] Platform 초기화 실패 (기존 닉네임 유지) : " + status);
                    IsDone = true;
                    return;
                }

                Debug.Log("[MetaProfile] Platform 초기화 완료");
                initialized = true;
                RequestUser();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[MetaProfile] Platform 초기화 실패 (기존 닉네임 유지)\n" + e);
            IsDone = true;
            return;
        }

        waiting = true;
    }

    private void Update()
    {
        if (!waiting)
            return;

        if (IsDone)
        {
            waiting = false;
            return;
        }

        // 한 프레임에 최대 0.1초만 셈 → 앱이 멈춰 있다가 돌아온 순간 바로 포기하지 않음
        waited += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        if (waited < RetryInterval)
            return;

        waited = 0f;

        if (!initialized)
        {
            Debug.LogWarning("[MetaProfile] Platform 초기화 응답이 없습니다. (기존 닉네임 유지)");
            IsDone = true;
            return;
        }

        if (requests >= MaxRequests)
        {
            Debug.LogWarning($"[MetaProfile] 프로필 조회를 {requests}번 요청했지만 응답이 없습니다. (기존 닉네임 유지)");
            IsDone = true;
            return;
        }

        Debug.LogWarning("[MetaProfile] 프로필 응답이 없어 다시 요청합니다.");
        RequestUser();
    }

    private void RequestUser()
    {
        requests++;
        waited = 0f;

        Debug.Log($"[MetaProfile] 프로필 조회 요청 ({requests}번째)");

        try
        {
            Users.GetLoggedInUser().OnComplete(OnUser);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[MetaProfile] 프로필 조회 요청 실패 (기존 닉네임 유지)\n" + e);
            IsDone = true;
        }
    }

    private void OnUser(Message<User> message)
    {
        if (DisplayName != null)
            return;   // 다시 요청한 것의 응답이 겹쳐서 온 경우

        if (message.IsError)
        {
            Fail("프로필 조회 실패", message.GetError());
            return;
        }

        User user = message.Data;

        string name = user != null ? user.DisplayName : null;
        if (string.IsNullOrWhiteSpace(name) && user != null)
            name = user.OculusID;

        if (string.IsNullOrWhiteSpace(name))
        {
            Debug.LogWarning(
                "[MetaProfile] 프로필 이름이 비어 있습니다. " +
                "대시보드 Data Use Checkup에서 User ID / User Profile 사용이 승인됐는지 확인하세요. (기존 닉네임 유지)");
            IsDone = true;
            return;
        }

        DisplayName = name.Trim();
        IsDone = true;

        Debug.Log("[MetaProfile] 프로필 이름 : " + DisplayName);

        StartCoroutine(ApplyWhenLoaded());
    }

    /// <summary>게임 데이터(SaveManager) 로드가 끝나면 닉네임에 반영</summary>
    private IEnumerator ApplyWhenLoaded()
    {
        while (SaveManager.Instance == null || !SaveManager.Instance.IsLoaded)
            yield return null;

        if (SaveManager.Instance.ApplyProfileNickname(DisplayName))
            NicknameChanged?.Invoke(DisplayName);
    }

    private static void Fail(string what, Error error)
    {
        Debug.LogWarning(
            $"[MetaProfile] {what} (기존 닉네임 유지) : " +
            (error != null ? $"{error.Code} {error.Message}" : "알 수 없는 오류"));

        IsDone = true;
    }
}
