using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public FadeManager fadeManager;
    public GameObject exitPanel;

    [Header("Canvas")]
    public GameObject mainMenuCanvas;
    public GameObject gameCanvas;

    private void Start()
    {
        if (mainMenuCanvas != null)
            mainMenuCanvas.SetActive(true);

        if (gameCanvas != null)
            gameCanvas.SetActive(false);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        Debug.Log("게임 종료");
#else
        Application.Quit();
#endif
    }

    public void ShowExitPanel()
    {
        if (exitPanel != null)
            exitPanel.SetActive(true);
    }

    public void HideExitPanel()
    {
        if (exitPanel != null)
            exitPanel.SetActive(false);
    }

    public void GoToMain()
    {
        Debug.Log("Play 버튼 실행");

        if (mainMenuCanvas != null)
            mainMenuCanvas.SetActive(false);

        if (gameCanvas != null)
            gameCanvas.SetActive(true);

        fadeManager.LoadScene("SampleScene");
    }

    public void GoToSkinSetting()
    {
        fadeManager.LoadScene("SkinScene");
    }

    public void GoToMainSetting()
    {
        fadeManager.LoadScene("SettingScene");
    }

    public void GoToLeaderboard()
    {
        fadeManager.LoadScene("LeaderBoardScene");
    }
}