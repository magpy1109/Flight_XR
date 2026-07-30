using UnityEngine;

public class SkinSceneBack : MonoBehaviour
{
    public FadeManager fadeManager;
    
    
    public void GoToMainMenu()
    {
        fadeManager.LoadScene("MainMenuScene");
    }
}
