using System.Collections;
using UnityEngine;
using Firebase.Auth;
using Firebase.Extensions;

public class FirebaseAuthManager : MonoBehaviour
{
    public static FirebaseAuthManager Instance;

    private FirebaseAuth auth;

    public FirebaseUser CurrentUser { get; private set; }

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
        // Firebase 의존성 초기화 완료 대기
        while (
            FirebaseManager.Instance == null ||
            !FirebaseManager.Instance.IsInitialized)
        {
            yield return null;
        }

        Debug.Log("FirebaseAuthManager : Firebase 초기화 확인");

        auth = FirebaseAuth.DefaultInstance;

        SignInAnonymously();
    }

    public void SignInAnonymously()
    {
        if (auth == null)
        {
            Debug.LogError("FirebaseAuth가 초기화되지 않았습니다.");
            return;
        }

        Debug.Log("익명 로그인 시작");

        auth.SignInAnonymouslyAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("익명 로그인 실패");
                    Debug.LogError(task.Exception);
                    return;
                }

                CurrentUser = task.Result.User;

                Debug.Log("로그인 성공");
                Debug.Log("UserId : " + CurrentUser.UserId);

                if (FirestoreManager.Instance != null)
                {
                    FirestoreManager.Instance.OnLoginCompleted();
                }
                else
                {
                    Debug.LogError(
                        "FirestoreManager.Instance가 없습니다."
                    );
                }
            });
    }
}