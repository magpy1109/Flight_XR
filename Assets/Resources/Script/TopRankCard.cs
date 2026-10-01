using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopRankCard : MonoBehaviour
{
    public Image playerIcon;

    public TMP_Text playerNameText;
    public TMP_Text distanceText;

    /// <summary>거리 기록 표시 (예전 방식)</summary>
    public void Setup(string playerName, float distance)
    {
        SetupText(playerName, distance.ToString("N1") + "m");
    }

    /// <summary>기록 글자를 그대로 표시 ("1,234.5m" / "1,230점")</summary>
    public void SetupText(string playerName, string valueText)
    {
        if (playerNameText != null)
            playerNameText.text = playerName;

        if (distanceText != null)
        {
            distanceText.textWrappingMode = TextWrappingModes.NoWrap;   // "1,230점" 등이 두 줄로 깨지지 않게
            distanceText.text = valueText;
        }
    }
}
