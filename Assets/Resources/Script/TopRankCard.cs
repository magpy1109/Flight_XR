using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopRankCard : MonoBehaviour
{
    public Image playerIcon;

    public TMP_Text playerNameText;
    public TMP_Text distanceText;

    public void Setup(string playerName, float distance)
    {
        playerNameText.text = playerName;
        distanceText.text = distance.ToString("N1") + "m";
    }
}