using UnityEngine;
using Firebase;
using Firebase.Extensions;

/// <summary>
/// Firebase 초기화.
///
/// - 의존성 확인(CheckAndFixDependencies) 후 기본 앱(FirebaseApp.DefaultInstance)까지 실제로 만들어 본다.
/// - 프로젝트 설정 파일이 빌드에 없으면(Assets/google-services.json 누락) 앱 생성이 실패한다.
///   예전에는 "초기화 성공"으로 표시된 뒤 로그인 / 기록 저장 때마다 예외가 났음
///   → 이제는 초기화 실패로 표시하고, 다른 스크립트는 Firebase를 건드리지 않는다. (게임은 그대로 진행)
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance;

    /// <summary>Firebase를 사용할 수 있는지</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>초기화가 실패로 끝났는지 (설정 파일 누락 등)</summary>
    public bool HasFailed { get; private set; }

    /// <summary>어디서든 확인용 : Firebase를 사용할 수 있는지</summary>
    public static bool Ready => Instance != null && Instance.IsInitialized;

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
            return;
        }

        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    HasFailed = true;
                    Debug.LogError("❌ Firebase 의존성 확인 실패 : " + task.Exception);
                    return;
                }

                DependencyStatus status = task.Result;

                if (status != DependencyStatus.Available)
                {
                    HasFailed = true;
                    Debug.LogError($"❌ Firebase 초기화 실패 : {status}");
                    return;
                }

                // 기본 앱을 실제로 만들어 봄 (설정 파일이 없으면 여기서 실패)
                try
                {
                    FirebaseApp app = FirebaseApp.DefaultInstance;
                    IsInitialized = app != null;
                }
                catch (System.Exception e)
                {
                    IsInitialized = false;
                    HasFailed = true;
                    Debug.LogError(
                        "❌ Firebase 앱을 만들지 못했습니다. Assets/google-services.json 파일이 프로젝트에 있는지 확인하세요.\n" +
                        "(Firebase 콘솔 > 프로젝트 설정 > Android 앱(com.dmu.flight) > google-services.json 다운로드 → Assets 폴더에 넣고 다시 빌드)\n" + e);
                    return;
                }

                if (IsInitialized)
                    Debug.Log("✅ Firebase 초기화 성공");
            });
    }
}
