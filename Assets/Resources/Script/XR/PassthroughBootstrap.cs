using UnityEngine;
using Meta.XR.MRUtilityKit;

public class PassthroughBootstrap : MonoBehaviour
{
    [Header("Passthrough")]
    [SerializeField] private OVRPassthroughLayer passthroughLayer;

    [Header("MRUK")]
    [SerializeField] private bool waitForMRUK = false;

    private bool passthroughStarted;

    private void Awake()
    {
        if (passthroughLayer == null)
        {
            passthroughLayer =
                GetComponentInChildren<OVRPassthroughLayer>(true);
        }

        if (passthroughLayer != null)
        {
            // 시작할 때는 꺼둔다.
            passthroughLayer.enabled = false;

            // 실제 HMD에 표시되었을 때 호출
            passthroughLayer.passthroughLayerResumed
                .AddListener(OnPassthroughLayerResumed);
        }
        else
        {
            Debug.LogError("OVRPassthroughLayer를 찾을 수 없습니다.");
        }
    }

    private void Start()
    {
        if (OVRManager.instance == null)
        {
            Debug.LogError("OVRManager.instance가 없습니다.");
            return;
        }

        // MainMenu처럼 MRUK가 필요하지 않은 씬에서는
        // 바로 Passthrough를 시작한다.
        if (waitForMRUK)
        {
            WaitForMRUK();
        }
        else
        {
            StartPassthrough();
        }
    }

    private void WaitForMRUK()
    {
        if (MRUK.Instance == null)
        {
            Debug.Log("MRUK 대기 중...");
            Invoke(nameof(WaitForMRUK), 0.1f);
            return;
        }

        Debug.Log(
            $"MRUK 발견 / Initialized = {MRUK.Instance.IsInitialized}"
        );

        MRUK.Instance.RegisterSceneLoadedCallback(OnMRUKSceneLoaded);
    }

    private void OnMRUKSceneLoaded()
    {
        Debug.Log("=== MRUK SCENE LOADED ===");

        StartPassthrough();
    }

    private void StartPassthrough()
    {
        if (passthroughStarted)
            return;

        if (OVRManager.instance == null)
        {
            Debug.LogError("OVRManager.instance가 없습니다.");
            return;
        }

        if (passthroughLayer == null)
        {
            Debug.LogError("OVRPassthroughLayer가 없습니다.");
            return;
        }

        passthroughStarted = true;

        Debug.Log("=== PASSTHROUGH START ===");

        // Insight Passthrough 활성화
        OVRManager.instance.isInsightPassthroughEnabled = true;

        // 실제 Passthrough Layer 활성화
        passthroughLayer.enabled = true;
    }

    private void OnPassthroughLayerResumed(
        OVRPassthroughLayer layer)
    {
        Debug.Log("=== PASSTHROUGH RESUMED ===");
        Debug.Log("패스스루가 실제 HMD 화면에 표시되었습니다.");
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(WaitForMRUK));

        if (passthroughLayer != null)
        {
            passthroughLayer.passthroughLayerResumed
                .RemoveListener(OnPassthroughLayerResumed);
        }
    }
}