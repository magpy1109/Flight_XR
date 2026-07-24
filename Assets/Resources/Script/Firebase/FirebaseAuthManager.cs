using UnityEngine;
using Firebase.Auth;
using Firebase.Extensions;

public class FirebaseAuthManager : MonoBehaviour
{
    public static FirebaseAuthManager Instance;

    private FirebaseAuth auth;

    public FirebaseUser CurrentUser { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        SignInAnonymously();
    }

    public void SignInAnonymously()
    {
        Debug.Log("익명 로그인 시작");

        auth.SignInAnonymouslyAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError(task.Exception);
                    return;
                }

                CurrentUser = task.Result.User;

                Debug.Log("로그인 성공");
                Debug.Log(CurrentUser.UserId);
            });
    }
}