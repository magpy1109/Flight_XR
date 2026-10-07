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

    private bool colorSaved;
    private Color nameColor;

    /// <summary>내 카드 강조 (닉네임 색). 해제하면 원래 색으로</summary>
    public void SetHighlight(bool on, Color color)
    {
        if (playerNameText == null)
            return;

        if (!colorSaved)
        {
            colorSaved = true;
            nameColor = playerNameText.color;
        }

        playerNameText.color = on ? color : nameColor;
    }

    /// <summary>기록 글자를 그대로 표시 ("1,234.5m" / "1,230점")</summary>
    public void SetupText(string playerName, string valueText)
    {
        if (playerNameText != null)
        {
            // 닉네임이 길어도 카드 밖으로 넘치지 않게 : 한 줄, 글자 크기 자동 축소
            if (!playerNameText.enableAutoSizing)
            {
                playerNameText.textWrappingMode = TextWrappingModes.NoWrap;
                playerNameText.fontSizeMax = playerNameText.fontSize;
                playerNameText.fontSizeMin = playerNameText.fontSize * 0.55f;
                playerNameText.enableAutoSizing = true;
            }

            playerNameText.text = playerName;
        }

        if (distanceText != null)
        {
            distanceText.textWrappingMode = TextWrappingModes.NoWrap;   // "1,230점" 등이 두 줄로 깨지지 않게
            distanceText.text = valueText;
        }
    }
}
