using UnityEngine;

public class PersistentXRRoot : MonoBehaviour
{
    private static PersistentXRRoot instance;

    [SerializeField]
    private OVRManager ovrManager;

    [SerializeField]
    private OVRPassthroughLayer passthroughLayer;

    private void Awake()
    {
        // 중복 방지
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // 씬이 바뀌어도 유지
        DontDestroyOnLoad(gameObject);

        // Inspector에서 연결하지 않았다면 자동 검색
        if (ovrManager == null)
            ovrManager = GetComponent<OVRManager>();

        if (passthroughLayer == null)
            passthroughLayer =
                GetComponentInChildren<OVRPassthroughLayer>(true);

        // Passthrough 활성화
        if (ovrManager != null)
            ovrManager.isInsightPassthroughEnabled = true;

        if (passthroughLayer != null)
            passthroughLayer.enabled = true;

        Debug.Log("=== Persistent XR Root Started ===");
        Debug.Log(
            $"Passthrough Initialized = {OVRManager.IsInsightPassthroughInitialized()}"
        );
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}