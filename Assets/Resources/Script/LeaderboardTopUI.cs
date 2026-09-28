using UnityEngine;
using TMPro;

public class LeaderboardTopUI : MonoBehaviour
{
    [Header("순위")]
    [SerializeField] private TextMeshProUGUI rankText;

    [Header("닉네임")]
    [SerializeField] private TextMeshProUGUI nicknameText;

    [Header("최대 거리")]
    [SerializeField] private TextMeshProUGUI distanceText;

    public void SetData(LeaderboardEntry entry)
    {
        if (entry == null)
            return;

        if (rankText != null)
            rankText.text = entry.rank + "";

        if (nicknameText != null)
            nicknameText.text = entry.nickname;

        if (distanceText != null)
            distanceText.text =
                entry.bestDistance.ToString("F2") + "m";
    }
}