using UnityEngine;

public class PanelPositionManager : MonoBehaviour
{
    public static PanelPositionManager Instance { get; private set; }

    public bool HasSavedPosition { get; private set; } = false;
    public Vector3 SavedPosition { get; private set; }
    public Quaternion SavedRotation { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SavePosition(Vector3 position, Quaternion rotation)
    {
        SavedPosition = position;
        SavedRotation = rotation;
        HasSavedPosition = true;
    }
}