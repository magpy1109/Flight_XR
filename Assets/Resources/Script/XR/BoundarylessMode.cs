using UnityEngine;

/// <summary>
/// 경계(가디언) 없이 플레이하기 (Meta Boundaryless / Boundary Visibility API).
///
/// Quest는 설정한 경계(룸스케일) 밖으로 나가면 앱 화면을 가리고 패스스루 + 경계를 보여준다.
/// 이 게임은 항상 패스스루(MR) 화면이므로, 패스스루가 켜져 있는 동안 경계 표시를 끄도록 요청한다.
/// → 경계 밖으로 걸어가도 게임 화면이 계속 보인다.
///
/// 필요 설정 (함께 적용됨)
/// - Oculus Project Config : Boundary Visibility Support = Required
/// - AndroidManifest : com.oculus.feature.BOUNDARYLESS_APP
/// - 헤드셋(Quest)에 직접 설치한 APK에서만 동작 (Link / 에디터에서는 경계가 그대로 보임)
///
/// 자동 생성되므로 어떤 오브젝트에도 붙일 필요 없음.
/// </summary>
public class BoundarylessMode : MonoBehaviour
{
    private const float CheckInterval = 0.5f;

    private float nextCheck;
    private bool lastRequested;
    private bool loggedResult;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (FindFirstObjectByType<BoundarylessMode>() != null)
            return;

        GameObject go = new GameObject("[Boundaryless]");
        DontDestroyOnLoad(go);
        go.AddComponent<BoundarylessMode>();

        OVRManager.BoundaryVisibilityChanged += OnBoundaryVisibilityChanged;
    }

    private static void OnBoundaryVisibilityChanged(OVRPlugin.BoundaryVisibility visibility)
    {
        Debug.Log($"[Boundaryless] 시스템 경계 표시 상태 : {visibility}");
    }

    private void Update()
    {
        if (Time.unscaledTime < nextCheck)
            return;

        nextCheck = Time.unscaledTime + CheckInterval;

        OVRManager manager = OVRManager.instance;
        if (manager == null)
            return;

        // 패스스루가 실제로 켜져 있을 때만 경계 숨김을 요청할 수 있다 (시스템 규칙)
        bool passthroughOn = manager.isInsightPassthroughEnabled && IsAnyPassthroughLayerActive();

        if (manager.shouldBoundaryVisibilityBeSuppressed != passthroughOn || lastRequested != passthroughOn)
        {
            manager.shouldBoundaryVisibilityBeSuppressed = passthroughOn;
            lastRequested = passthroughOn;
            loggedResult = false;

            Debug.Log($"[Boundaryless] 경계 숨김 요청 = {passthroughOn}");
        }

        if (!loggedResult && passthroughOn)
        {
            loggedResult = true;
            Debug.Log($"[Boundaryless] 경계 숨김 적용 = {manager.isBoundaryVisibilitySuppressed}");
        }
    }

    private static OVRPassthroughLayer[] layers;
    private static float nextLayerScan;

    private static bool IsAnyPassthroughLayerActive()
    {
        if (layers == null || Time.unscaledTime >= nextLayerScan)
        {
            layers = FindObjectsByType<OVRPassthroughLayer>(FindObjectsSortMode.None);
            nextLayerScan = Time.unscaledTime + 2f;
        }

        foreach (OVRPassthroughLayer layer in layers)
        {
            if (layer != null && layer.isActiveAndEnabled)
                return true;
        }

        return false;
    }
}
