using UnityEngine;

public class SpawnPointResolver : MonoBehaviour
{
    public static SpawnPointResolver Instance { get; private set; }

    [Header("XR References")]
    [SerializeField] private Transform rightHandAnchor;
    [SerializeField] private Transform centerEyeAnchor;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Transform GetSpawnTransform()
    {
    #if UNITY_EDITOR
        if (centerEyeAnchor == null)
        {
            Debug.LogError("CenterEyeAnchor가 연결되지 않았습니다.");
            return transform;
        }

        return centerEyeAnchor;
    #else
        if (rightHandAnchor == null)
        {
            Debug.LogError("RightHandAnchor가 연결되지 않았습니다.");
            return transform;
        }

        return rightHandAnchor;
    #endif
    }
}