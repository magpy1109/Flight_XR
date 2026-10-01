using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankRow : MonoBehaviour
{
    public TMP_Text rankText;
    public TMP_Text playerNameText;
    public TMP_Text distanceText;
    public Image planeIcon;

    /// <summary>거리 기록 표시 (예전 방식)</summary>
    public void SetData(int rank, string playerName, float distance, Sprite icon)
    {
        SetDataText(rank, playerName, distance.ToString("N1") + "m", icon);
    }

    /// <summary>순위 칸에 글자 표시 (불러오는 중 "…" 등)</summary>
    public void SetRankLabel(string label)
    {
        if (rankText != null)
            rankText.text = label;
    }

    /// <summary>기록 글자를 그대로 표시 ("1,234.5m" / "1,230점"). 순위가 0 이하면 "-"</summary>
    public void SetDataText(int rank, string playerName, string valueText, Sprite icon)
    {
        if (rankText != null)
            rankText.text = rank > 0 ? rank.ToString() : "-";

        if (playerNameText != null)
            playerNameText.text = playerName;

        if (distanceText != null)
        {
            distanceText.textWrappingMode = TextWrappingModes.NoWrap;   // "1,230점" 등이 두 줄로 깨지지 않게
            distanceText.text = valueText;
        }

        if (icon != null && planeIcon != null)
            planeIcon.sprite = icon;
    }
}
