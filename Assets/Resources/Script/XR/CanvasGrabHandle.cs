using Oculus.Interaction;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 메뉴 Canvas 아래에 메타 기본 창처럼 "손잡이 막대"를 만들고,
/// 손잡이를 레이로 가리키고 트리거를 누른 채 움직이면 Canvas를 옮길 수 있게 한다.
///
/// - 옮기는 동안 Canvas는 항상 사용자를 바라보도록 회전한다.
/// - 놓으면 그 위치를 기억해서 다음 씬의 메뉴도 같은 자리에 나온다. (MainMenuCanvasPositioner)
/// - 헤드셋 없이 에디터에서 실행하면 마우스로 끌어서 옮길 수 있다.
///
/// MainMenuCanvasPositioner가 붙은 Canvas에 자동으로 추가된다. (씬 파일 수정 없음)
/// </summary>
public class CanvasGrabHandle : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private static readonly Color NormalColor = new Color(1f, 1f, 1f, 0.55f);
    private static readonly Color HoverColor = new Color(1f, 1f, 1f, 0.9f);
    private static readonly Color GrabColor = new Color(0.047f, 0.549f, 0.914f, 1f); // #0C8CE9

    private Transform canvasTransform;
    private Image image;

    private bool hovering;
    private bool grabbing;

    private float grabDistance;
    private Vector3 localOffset; // 잡은 지점 → Canvas 중심 (Canvas 기준 좌표)
    private RayInteractor grabbingRay;

    // ---------- 자동 설치 ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        InstallAll();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallAll();
    }

    private static void InstallAll()
    {
        MainMenuCanvasPositioner[] positioners =
            FindObjectsByType<MainMenuCanvasPositioner>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (MainMenuCanvasPositioner positioner in positioners)
        {
            Canvas canvas = positioner.GetComponent<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
                continue;

            if (canvas.GetComponentInChildren<CanvasGrabHandle>(true) != null)
                continue;

            Create(canvas);
        }
    }

    private static void Create(Canvas canvas)
    {
        RectTransform canvasRect = (RectTransform)canvas.transform;

        GameObject go = new GameObject("CanvasGrabHandle", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvasRect, false);
        go.transform.SetAsLastSibling();

        // Canvas 아래쪽 가운데, 살짝 바깥에 가로 막대
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -25f);
        rect.sizeDelta = new Vector2(320f, 26f);

        Image img = go.GetComponent<Image>();
        img.color = NormalColor;
        img.raycastTarget = true;

        // 손잡이를 잡기 쉽도록 보이지 않는 넓은 판정 영역
        GameObject hit = new GameObject("HitArea", typeof(RectTransform), typeof(Image));
        hit.transform.SetParent(go.transform, false);
        RectTransform hitRect = (RectTransform)hit.transform;
        hitRect.anchorMin = Vector2.zero;
        hitRect.anchorMax = Vector2.one;
        hitRect.offsetMin = new Vector2(-40f, -30f);
        hitRect.offsetMax = new Vector2(40f, 30f);
        Image hitImage = hit.GetComponent<Image>();
        hitImage.color = new Color(1f, 1f, 1f, 0f);
        hitImage.raycastTarget = true;

        CanvasGrabHandle handle = go.AddComponent<CanvasGrabHandle>();
        handle.canvasTransform = canvasRect;
        handle.image = img;
    }

    // ---------- 포인터 이벤트 ----------

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        RefreshColor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        RefreshColor();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!TryGetPointerRay(out Ray ray, out RayInteractor rayInteractor))
            return;

        // 잡은 지점 = 레이와 Canvas 평면의 교점
        UnityEngine.Plane plane = new UnityEngine.Plane(canvasTransform.forward, canvasTransform.position);
        if (!plane.Raycast(ray, out float enter))
            enter = Vector3.Distance(ray.origin, canvasTransform.position);

        grabDistance = Mathf.Max(0.3f, enter);
        Vector3 grabPoint = ray.GetPoint(grabDistance);

        localOffset = Quaternion.Inverse(canvasTransform.rotation) * (canvasTransform.position - grabPoint);
        grabbingRay = rayInteractor;
        grabbing = true;

        RefreshColor();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        EndGrab();
    }

    private void OnDisable()
    {
        EndGrab();
    }

    // ---------- 이동 ----------

    private void LateUpdate()
    {
        if (!grabbing)
            return;

        // 트리거(마우스 버튼)를 놓쳤을 때 대비
        if (!IsStillPressed())
        {
            EndGrab();
            return;
        }

        if (!TryGetPointerRay(out Ray ray, out _))
            return;

        Transform eye = Camera.main != null ? Camera.main.transform : null;

        Vector3 grabPoint = ray.GetPoint(grabDistance);

        Quaternion rotation = eye != null
            ? MainMenuCanvasPositioner.FacingRotation(grabPoint, eye.position)
            : canvasTransform.rotation;

        canvasTransform.SetPositionAndRotation(grabPoint + rotation * localOffset, rotation);
    }

    private void EndGrab()
    {
        if (!grabbing)
            return;

        grabbing = false;
        grabbingRay = null;

        if (canvasTransform != null)
        {
            MainMenuCanvasPositioner.SavePose(canvasTransform.position, canvasTransform.rotation);
            Debug.Log($"[CanvasGrabHandle] 메뉴 위치 저장 {canvasTransform.position}");
        }

        RefreshColor();
    }

    private void RefreshColor()
    {
        if (image == null)
            return;

        image.color = grabbing ? GrabColor : (hovering ? HoverColor : NormalColor);
    }

    // ---------- 레이 ----------

    /// <summary>지금 Canvas를 가리키는 레이 (VR : 공식 컨트롤러 레이, 에디터 : 마우스)</summary>
    private bool TryGetPointerRay(out Ray ray, out RayInteractor rayInteractor)
    {
        rayInteractor = null;

        if (OfficialRay.IsActive && OfficialRay.RigInstance != null && OfficialRay.IsHmdPresent())
        {
            // 잡는 중이면 잡은 레이를 계속 사용
            if (grabbingRay != null && grabbingRay.isActiveAndEnabled)
            {
                rayInteractor = grabbingRay;
                ray = grabbingRay.Ray;
                return true;
            }

            RayInteractor best = null;

            foreach (RayInteractor candidate in OfficialRay.RigInstance.GetComponentsInChildren<RayInteractor>(false))
            {
                if (!candidate.isActiveAndEnabled)
                    continue;

                if (candidate.State == InteractorState.Select)
                {
                    best = candidate;
                    break;
                }

                if (best == null && candidate.State == InteractorState.Hover)
                    best = candidate;
            }

            if (best != null)
            {
                rayInteractor = best;
                ray = best.Ray;
                return true;
            }

            ray = default(Ray);
            return false;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            ray = default(Ray);
            return false;
        }

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        ray = cam.ScreenPointToRay(Input.mousePosition);
        return true;
#else
        ray = default(Ray);
        return false;
#endif
    }

    private bool IsStillPressed()
    {
        if (grabbingRay != null)
            return grabbingRay.isActiveAndEnabled && grabbingRay.State == InteractorState.Select;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.leftButton.isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(0);
#else
        return true;
#endif
    }
}
