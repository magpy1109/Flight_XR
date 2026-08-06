using UnityEngine;
using UnityEngine.InputSystem;

public class TestGameResult : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current.pKey.wasPressedThisFrame)
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
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            SkinManager.Instance.UnlockSkin("gold");
        }
    }
    
}