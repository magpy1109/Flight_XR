using UnityEngine;

public class LeaderBoardController : MonoBehaviour
{
    [Header("Fade")]
    public FadeManager fadeManager;

    public void GoToMainMenu()
    {
        fadeManager.LoadScene("MainMenuScene");
    }
}