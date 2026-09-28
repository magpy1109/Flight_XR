using UnityEngine;

/// <summary>
/// 메인메뉴씬의 카메라 리그 루트. 씬이 바뀌어도 유지된다(DontDestroyOnLoad).
/// 메인메뉴로 다시 돌아와서 두 번째 리그가 로드되면, 그 리그는 즉시 비활성화 후 파괴된다.
/// 다른 컴포넌트(OVRManager 등)보다 먼저 Awake 되도록 실행 순서를 가장 앞으로 둔다.
/// </summary>
[DefaultExecutionOrder(-32000)]
public class PersistentXRRoot : MonoBehaviour
{
    public static PersistentXRRoot Instance { get; private set; }

    [SerializeField]
    private OVRManager ovrManager;

    [SerializeField]
    private OVRPassthroughLayer passthroughLayer;

    private void Awake()
    {
        // 중복 방지 : 메인메뉴로 돌아왔을 때 새로 로드된 리그 제거
        if (Instance != null && Instance != this)
        {
            // 자식(OVRManager, 카메라, UI 레이 등)이 동작하기 전에 꺼버린다.
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 씬이 바뀌어도 유지
        DontDestroyOnLoad(gameObject);

        // Inspector에서 연결하지 않았다면 자동 검색
        if (ovrManager == null)
            ovrManager = GetComponentInChildren<OVRManager>(true);

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
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
