using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private PlaneLauncher launcher;

    void Update()
    {
        if (FlightInputManager.Instance.LaunchPressed)
        {
            launcher.Launch();
        }
    }
}