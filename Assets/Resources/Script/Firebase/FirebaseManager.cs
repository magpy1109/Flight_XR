using UnityEngine;
using Firebase;
using Firebase.Extensions;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance;

    public bool IsInitialized { get; private set; }

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
                DependencyStatus status = task.Result;

                if (status == DependencyStatus.Available)
                {
                    IsInitialized = true;
                    Debug.Log("✅ Firebase 초기화 성공");
                }
                else
                {
                    Debug.LogError($"❌ Firebase 초기화 실패 : {status}");
                }
            });
    }
}