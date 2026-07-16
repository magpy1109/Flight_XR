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
        // PC 테스트는 카메라 기준
        return centerEyeAnchor;
#else
        // Quest는 오른손 기준
        return rightHandAnchor;
#endif
    }
}