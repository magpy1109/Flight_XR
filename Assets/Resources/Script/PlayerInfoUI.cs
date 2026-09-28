using System.Collections;
using UnityEngine;
using TMPro;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

public class PlayerInfoUI : MonoBehaviour
{
    [Header("닉네임")]
    [SerializeField] private TextMeshProUGUI nicknameText;

    [Header("등수")]
    [SerializeField] private TextMeshProUGUI rankText;

    private FirebaseFirestore db;

    private IEnumerator Start()
    {
        // SaveManager가 생성될 때까지 대기
        while (SaveManager.Instance == null)
        {
            yield return null;
        }

        // 게임 데이터 로드 완료까지 대기
        while (!SaveManager.Instance.IsLoaded)
        {
            yield return null;
        }

        Debug.Log("PlayerInfoUI : 게임 데이터 로드 확인");

        db = FirebaseFirestore.DefaultInstance;

        UpdateNickname();
        UpdateRank();
    }

    private void UpdateNickname()
    {
        if (SaveManager.Instance.CurrentUser == null)
        {
            Debug.LogWarning("PlayerInfoUI : CurrentUser가 없습니다.");
            return;
        }

        string nickname =
            SaveManager.Instance.CurrentUser.nickname;

        if (string.IsNullOrEmpty(nickname))
            nickname = "Player";

        if (nicknameText != null)
            nicknameText.text = nickname;
    }

    private void UpdateRank()
    {
        FirebaseUser user =
            FirebaseAuth.DefaultInstance.CurrentUser;

        if (user == null)
        {
            Debug.LogWarning("PlayerInfoUI : 로그인된 유저가 없습니다.");

            if (rankText != null)
                rankText.text = "-";

            return;
        }

        if (SaveManager.Instance.CurrentStats == null)
        {
            Debug.LogWarning("PlayerInfoUI : CurrentStats가 없습니다.");

            if (rankText != null)
                rankText.text = "-";

            return;
        }

        int myBestScore =
            SaveManager.Instance.CurrentStats.best_score;

        db.Collection("user_stats")
            .WhereGreaterThan("best_score", myBestScore)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError(
                        "등수 조회 실패 : " + task.Exception
                    );

                    if (rankText != null)
                        rankText.text = "-";

                    return;
                }

                int rank = task.Result.Count + 1;

                if (rankText != null)
                    rankText.text = rank + "위";

                Debug.Log(
                    $"현재 최고 점수 : {myBestScore} / 등수 : {rank}위"
                );
            });
    }
}