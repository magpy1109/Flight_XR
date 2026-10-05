using System.Collections;
using UnityEngine;
using Oculus.Platform;
using Oculus.Platform.Models;

/// <summary>
/// 닉네임 = Meta(Quest) 계정 프로필 이름으로 고정.
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

    /// <summary>받아온 프로필 이름 (아직 못 받았으면 null)</summary>
    public static string DisplayName { get; private set; }

    /// <summary>프로필 조회가 끝났는지 (성공 / 실패 모두)</summary>
    public static bool IsDone { get; private set; }

    /// <summary>닉네임이 프로필 이름으로 바뀜 (화면 갱신용)</summary>
    public static event System.Action<string> NicknameChanged;

    private const float Timeout = 20f;

    private static MetaProfile instance;

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
                RequestUser();
            }
            else
            {
                Core.AsyncInitialize(AppId).OnComplete(message =>
                {
                    if (message.IsError)
                    {
                        Fail("Platform 초기화 실패", message.GetError());
                        return;
                    }

                    RequestUser();
                });
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[MetaProfile] Platform 초기화 실패 (기존 닉네임 유지)\n" + e);
            IsDone = true;
            return;
        }

        StartCoroutine(GiveUpAfter(Timeout));
    }

    private void RequestUser()
    {
        Users.GetLoggedInUser().OnComplete(message =>
        {
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
        });
    }

    /// <summary>게임 데이터(SaveManager) 로드가 끝나면 닉네임에 반영</summary>
    private IEnumerator ApplyWhenLoaded()
    {
        while (SaveManager.Instance == null || !SaveManager.Instance.IsLoaded)
            yield return null;

        if (SaveManager.Instance.ApplyProfileNickname(DisplayName))
            NicknameChanged?.Invoke(DisplayName);
    }

    private IEnumerator GiveUpAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);

        if (!IsDone)
        {
            Debug.LogWarning("[MetaProfile] Meta 프로필 응답이 없습니다. (기존 닉네임 유지)");
            IsDone = true;
        }
    }

    private static void Fail(string what, Error error)
    {
        Debug.LogWarning(
            $"[MetaProfile] {what} (기존 닉네임 유지) : " +
            (error != null ? $"{error.Code} {error.Message}" : "알 수 없는 오류"));

        IsDone = true;
    }
}
