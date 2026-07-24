using UnityEngine;

public class TestGameResult : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("P 눌림!");

            GameResultManager.Instance.SaveResult(
                120,
                185.5f,
                42,
                31.4f,
                8
            );
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            SkinManager.Instance.UnlockSkin("gold");
        }
    }
    
}