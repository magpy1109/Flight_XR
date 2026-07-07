using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public FadeManager fadeManager;

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        Debug.Log("게임 종료");
#else
        Application.Quit();
#endif
    }

    public void GoToMain()
    {
        fadeManager.LoadScene("MainScene");
    }

    public void GoToSkinSetting()
    {
        fadeManager.LoadScene("SkinScene_FLIGHT");
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