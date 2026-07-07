using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankRow : MonoBehaviour
{
    public TMP_Text rankText;
    public TMP_Text playerNameText;
    public TMP_Text distanceText;
    public Image planeIcon;

    public void SetData(int rank, string playerName, float distance, Sprite icon)
    {
        rankText.text = rank.ToString();
        playerNameText.text = playerName;
        distanceText.text = distance.ToString("N1") + "m";

        if (icon != null)
        {
            planeIcon.sprite = icon;
        }
    }
}