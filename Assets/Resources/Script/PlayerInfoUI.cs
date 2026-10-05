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

        // 닉네임 = Meta 프로필 이름. 프로필이 늦게 도착하면 그때 다시 표시
        MetaProfile.NicknameChanged += OnNicknameChanged;
    }

    private void OnDestroy()
    {
        MetaProfile.NicknameChanged -= OnNicknameChanged;
    }

    private void OnNicknameChanged(string nickname)
    {
        if (nicknameText != null)
            nicknameText.text = nickname;
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

        // 메인 화면 등수 = 리더보드 거리 랭킹 기준 순위
        // (예전에는 최고 점수 기준이었는데 점수가 모두 0이라 다들 1위로 나왔음)
        float myBestDistance =
            SaveManager.Instance.CurrentStats.best_distance;

        LeaderboardQueries.GetRank(LeaderboardMode.Distance, myBestDistance, rank =>
        {
            if (rankText != null)
                rankText.text = rank > 0 ? rank + "위" : "-";

            Debug.Log(
                $"현재 최고 거리 : {myBestDistance:F1}m / 등수 : {rank}위"
            );
        });
    }
}