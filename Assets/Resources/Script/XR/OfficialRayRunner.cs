using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

/// <summary>
/// 공식 레이가 각 씬의 UI를 누를 수 있게 연결하는 관리자 (자동 생성, 씬이 바뀌어도 유지).
///
/// 1) 현재 씬의 EventSystem에 PointableCanvasModule(메타 공식 UI 입력 모듈)을 추가
///    - 헤드셋 실행 : 공식 모듈만 사용
///    - 헤드셋 없이 에디터 실행 : 기존 입력 모듈 유지 (마우스 테스트 가능)
/// 2) World Space Canvas마다 IsdkCanvasProxy를 만들어 레이가 맞을 수 있게 함
///    - 씬 Canvas, 카메라 리그에 붙은 GameCanvas, 나중에 켜지는 일시정지 Canvas 모두 포함
/// </summary>
public class OfficialRayRunner : MonoBehaviour
{
    private static OfficialRayRunner instance;

    private const float RescanInterval = 0.5f;

    private readonly Dictionary<Canvas, IsdkCanvasProxy> proxies = new Dictionary<Canvas, IsdkCanvasProxy>();
    private readonly List<Canvas> removeBuffer = new List<Canvas>();

    private float nextScanTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    public static void EnsureExists()
    {
        if (instance != null)
            return;

        GameObject go = new GameObject("[OfficialRay] Runner");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<OfficialRayRunner>();
    }

    /// <summary>씬이 바뀐 직후 다음 프레임에 바로 다시 연결</summary>
    public static void RequestRescan()
    {
        if (instance != null)
            instance.nextScanTime = 0f;
    }

    private void Update()
    {
        if (!OfficialRay.IsActive)
            return;

        // 일시정지(timeScale 0) 중에도 동작하도록 unscaled 시간 사용
        if (Time.unscaledTime < nextScanTime)
            return;

        nextScanTime = Time.unscaledTime + RescanInterval;
        Scan();
    }

    private void Scan()
    {
        if (!EnsureModule())
            return;

        // 씬이 바뀌어 사라진 Proxy 정리
        removeBuffer.Clear();
        foreach (KeyValuePair<Canvas, IsdkCanvasProxy> pair in proxies)
        {
            if (pair.Key == null || pair.Value == null)
                removeBuffer.Add(pair.Key);
        }
        foreach (Canvas canvas in removeBuffer)
            proxies.Remove(canvas);

        // 새 Canvas 연결
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Canvas canvas in canvases)
        {
            if (!IsTargetCanvas(canvas))
                continue;

            if (proxies.TryGetValue(canvas, out IsdkCanvasProxy existing) && existing != null)
                continue;

            // 씬에 속한 Canvas는 같은 씬에, 씬을 넘어가는 Canvas(GameCanvas 등)는 현재 씬에 만든다.
            // → 씬이 바뀌면 Proxy도 함께 정리되고, 새 씬에서 다시 연결된다.
            Scene scene = canvas.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded || scene.name == "DontDestroyOnLoad")
                scene = SceneManager.GetActiveScene();

            IsdkCanvasProxy proxy = IsdkCanvasProxy.Create(canvas, scene, OfficialRay.Settings.canvasHitPadding);
            if (proxy != null)
                proxies[canvas] = proxy;
        }
    }

    /// <summary>현재 EventSystem에 공식 UI 입력 모듈이 있게 한다. 준비되면 true</summary>
    private bool EnsureModule()
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
            return false;

        if (eventSystem.GetComponent<PointableCanvasModule>() != null)
            return true;

        // 이전 씬의 모듈이 아직 남아 있으면 다음 검사 때 추가 (모듈은 씬에 하나만 있어야 함)
        PointableCanvasModule[] existing =
            FindObjectsByType<PointableCanvasModule>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (existing.Length > 0)
            return false;

        bool hmd = OfficialRay.IsHmdPresent();

        PointableCanvasModule module = eventSystem.gameObject.AddComponent<PointableCanvasModule>();

        if (hmd)
        {
            // VR 실행 : 공식 모듈만 사용 (기존 XRUIInputModule 등은 자동으로 꺼짐)
            module.ExclusiveMode = true;
        }
        else
        {
            // 헤드셋 없이 에디터 실행 : 마우스로 테스트할 수 있게 기존 입력 모듈 유지
            module.ExclusiveMode = false;
            module.enabled = false;
        }

        Debug.Log($"[OfficialRay] {eventSystem.gameObject.scene.name} EventSystem에 공식 UI 입력 모듈 추가 (VR={hmd})");
        return true;
    }

    private static bool IsTargetCanvas(Canvas canvas)
    {
        if (canvas == null)
            return false;

        if (canvas.renderMode != RenderMode.WorldSpace)
            return false;

        // 하위 Canvas(드롭다운 목록 등)는 루트 Canvas가 처리
        Transform parent = canvas.transform.parent;
        if (parent != null && parent.GetComponentInParent<Canvas>(true) != null)
            return false;

        // 공식 리그 내부의 Canvas는 제외
        if (OfficialRay.RigInstance != null &&
            canvas.transform.IsChildOf(OfficialRay.RigInstance.transform))
            return false;

        // 에디터 미리보기용 등 씬에 없는 오브젝트 제외
        if (!canvas.gameObject.scene.IsValid())
            return false;

        return true;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
