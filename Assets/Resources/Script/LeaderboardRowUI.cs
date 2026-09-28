using UnityEngine;
using TMPro;

public class LeaderboardRowUI : MonoBehaviour
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
            rankText.text = entry.rank + "위";

        if (nicknameText != null)
            nicknameText.text = entry.nickname;

        if (distanceText != null)
            distanceText.text =
                entry.bestDistance.ToString("F2") + "m";
    }

    public void Clear()
    {
        if (rankText != null)
            rankText.text = "";

        if (nicknameText != null)
            nicknameText.text = "";

        if (distanceText != null)
            distanceText.text = "";
    }
}