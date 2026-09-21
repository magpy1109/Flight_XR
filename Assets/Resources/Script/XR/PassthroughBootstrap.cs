using UnityEngine;
using Meta.XR.MRUtilityKit;

public class PassthroughBootstrap : MonoBehaviour
{
    [SerializeField] private OVRPassthroughLayer passthroughLayer;

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
            // 시작할 때는 일단 패스스루 레이어를 꺼둔다.
            passthroughLayer.enabled = false;

            // 패스스루가 실제 화면에 표시된 순간 호출
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

        // 시작 시 자동 패스스루 방지
        OVRManager.instance.isInsightPassthroughEnabled = false;

        WaitForMRUK();
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

        // MRUK 씬이 이미 준비됐으면 즉시 콜백
        // 아직 준비되지 않았으면 Scene Loaded 시 호출
        MRUK.Instance.RegisterSceneLoadedCallback(OnMRUKSceneLoaded);
    }

    private void OnMRUKSceneLoaded()
    {
        if (passthroughStarted)
            return;

        passthroughStarted = true;

        Debug.Log("=== MRUK SCENE LOADED ===");

        StartPassthrough();
    }

    private void StartPassthrough()
    {
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

        Debug.Log("=== PASSTHROUGH START ===");

        // 1. Insight Passthrough 시스템 활성화
        OVRManager.instance.isInsightPassthroughEnabled = true;

        // 2. 실제 Passthrough Layer 활성화
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