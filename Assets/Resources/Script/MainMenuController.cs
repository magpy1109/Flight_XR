using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public FadeManager fadeManager;
    public GameObject exitPanel;

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
        exitPanel.SetActive(true);
    }

    public void HideExitPanel()
    {
        exitPanel.SetActive(false);
    }

    public void GoToMain()
    {
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