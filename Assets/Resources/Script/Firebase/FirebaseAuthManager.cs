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
        auth.SignInAnonymouslyAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("로그인 실패");
                    return;
                }

                CurrentUser = task.Result.User;

                Debug.Log("로그인 성공");

                Debug.Log(CurrentUser.UserId);
            });
    }
}