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
        // 오른손 기준
        if (rightHandAnchor != null)
        {
            Debug.Log("Spawn 위치 : RightHandAnchor");
            return rightHandAnchor;
        }

        // 오른손을 찾지 못하면 카메라 기준
        if (centerEyeAnchor != null)
        {
            Debug.LogWarning(
                "RightHandAnchor가 없어 CenterEyeAnchor를 사용합니다."
            );

            return centerEyeAnchor;
        }

        // 마지막 fallback
        if (Camera.main != null)
        {
            Debug.LogWarning(
                "XR Anchor가 없어 Main Camera를 사용합니다."
            );

            return Camera.main.transform;
        }

        Debug.LogError(
            "SpawnPointResolver : 사용할 Spawn Transform이 없습니다."
        );

        return transform;
    }
}