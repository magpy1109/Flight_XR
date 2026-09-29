using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.SceneManagement;

public class QuestUIRayInteractor : MonoBehaviour
{
    [SerializeField] private GraphicRaycaster graphicRaycaster;

    [Header("Canvas")]
    [SerializeField] private Canvas mainMenuCanvas;
    [SerializeField] private Canvas gameCanvas;

    private readonly List<RaycastResult> raycastResults =
        new List<RaycastResult>();

    [Header("References")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Camera targetCamera;

    [Header("Ray")]
    [SerializeField] private float rayLength = 5f;
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Reticle")]
    [SerializeField] private float reticleSize = 0.02f;

    private InputDevice rightHand;

    private Button currentButton;

    // 레이가 올라가 있는 슬라이더 (호버 효과용, 클릭은 하지 않음)
    private Slider currentSlider;

    // A 버튼을 누른 채 조작 중인 슬라이더
    private Slider draggingSlider;

    // 이번 프레임에 레이가 Canvas에 닿은 위치
    private bool lastHitCanvas;
    private Vector3 lastHitPoint;

    // A 버튼의 이전 프레임 상태
    private bool wasPressed;

    private GameObject reticle;

    private void Start()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        SetupCanvas(SceneManager.GetActiveScene().name);

        if (targetCamera == null)
        {
            Debug.LogError(
                "QuestUIRayInteractor: Camera를 찾을 수 없습니다."
            );
            return;
        }

        if (targetCanvas == null)
        {
            Debug.LogError(
                "QuestUIRayInteractor: 사용할 Canvas가 없습니다."
            );
            return;
        }

        // World Space Canvas가 사용할 카메라
        targetCanvas.worldCamera = targetCamera;

        if (graphicRaycaster == null)
        {
            graphicRaycaster = targetCanvas.GetComponent<GraphicRaycaster>();
        }

        if (graphicRaycaster == null)
        {
            Debug.LogError("Canvas에 GraphicRaycaster가 없습니다.");
            return;
        }

        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();

            if (lineRenderer == null)
                lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.positionCount = 2;
        lineRenderer.enabled = true;

        CreateReticle();

        Debug.Log("Quest UI Ray Interactor 시작");
    }

    private void Update()
    {
        GetRightHand();

        if (targetCanvas == null || targetCamera == null)
        {
            lastHitCanvas = false;

            DrawRay(
                transform.position,
                transform.position +
                transform.forward * rayLength
            );

            CheckButtonInput();
            return;
        }

        // ------------------------------------------------
        // 1. 컨트롤러에서 Ray 발사
        // ------------------------------------------------

        Ray ray = new Ray(
            transform.position,
            transform.forward
        );

        bool hitCanvas = TryGetCanvasHit(
            ray,
            out Vector3 hitPoint
        );

        lastHitCanvas = hitCanvas;
        lastHitPoint = hitPoint;

        Vector3 rayEnd = hitCanvas
            ? hitPoint
            : ray.origin + ray.direction * rayLength;

        DrawRay(ray.origin, rayEnd);

        // ------------------------------------------------
        // 2. Canvas를 맞추지 못한 경우
        // ------------------------------------------------

        if (!hitCanvas)
        {
            ClearHover();
            UpdateSliderHover(null);

            if (reticle != null)
                reticle.SetActive(false);

            CheckButtonInput();

            return;
        }

        // ------------------------------------------------
        // 3. Reticle 표시
        // ------------------------------------------------

        if (reticle != null)
        {
            reticle.SetActive(true);

            reticle.transform.position =
                hitPoint;

            reticle.transform.rotation =
                Quaternion.LookRotation(
                    targetCanvas.transform.forward
                );
        }

        // ------------------------------------------------
        // 4. 맞은 Button 찾기
        // ------------------------------------------------

        Button hitButton =
            FindButtonAtWorldPoint(hitPoint);

        UpdateHover(hitButton);

        // 버튼이 아니면 슬라이더 위인지 확인 (호버 확대 효과용)
        UpdateSliderHover(
            hitButton == null ? FindSliderAtWorldPoint(hitPoint) : null
        );

        // ------------------------------------------------
        // 5. A 버튼 확인
        // ------------------------------------------------

        CheckButtonInput();
    }

    private void GetRightHand()
    {
        if (rightHand.isValid)
            return;

        rightHand =
            InputDevices.GetDeviceAtXRNode(
                XRNode.RightHand
            );
    }

    private void CheckButtonInput()
    {
        bool isPressed = false;

        if (rightHand.isValid)
        {
            rightHand.TryGetFeatureValue(
                CommonUsages.primaryButton,
                out isPressed
            );
        }

        // 버튼을 누르는 순간만 실행
        if (isPressed && !wasPressed)
        {
            Debug.Log("Quest A 버튼 입력 감지");

            if (currentButton == null && currentSlider != null)
            {
                // 슬라이더 위에서 누르면 드래그 시작
                draggingSlider = currentSlider;
            }
            else
            {
                PressCurrentButton();
            }
        }

        // 누르고 있는 동안 슬라이더 값 변경
        if (isPressed && draggingSlider != null)
        {
            DragSlider(draggingSlider);
        }

        if (!isPressed)
        {
            draggingSlider = null;
        }

        wasPressed = isPressed;
    }

    private bool TryGetCanvasHit(
        Ray ray,
        out Vector3 hitPoint)
    {
        UnityEngine.Plane canvasPlane =
            new UnityEngine.Plane(
                targetCanvas.transform.forward,
                targetCanvas.transform.position
            );

        if (canvasPlane.Raycast(
            ray,
            out float enter))
        {
            Vector3 point =
                ray.GetPoint(enter);

            float distance =
                Vector3.Distance(
                    ray.origin,
                    point
                );

            // Ray 길이 확인
            if (distance <= rayLength)
            {
                RectTransform rect =
                    targetCanvas.transform
                        as RectTransform;

                if (rect != null)
                {
                    Vector2 localPoint;

                    RectTransformUtility
                        .ScreenPointToLocalPointInRectangle(
                            rect,
                            targetCamera.WorldToScreenPoint(point),
                            targetCamera,
                            out localPoint
                        );

                    if (rect.rect.Contains(localPoint))
                    {
                        hitPoint = point;
                        return true;
                    }
                }
            }
        }

        hitPoint = Vector3.zero;
        return false;
    }

    private Button FindButtonAtWorldPoint(Vector3 worldPoint)
    {
        Vector2 screenPoint =
            targetCamera.WorldToScreenPoint(worldPoint);

        Button[] buttons =
            targetCanvas.GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            if (!button.gameObject.activeInHierarchy)
                continue;

            if (!button.interactable)
                continue;

            RectTransform rect =
                button.GetComponent<RectTransform>();

            if (rect == null)
                continue;

            // Button의 전체 RectTransform 영역을 클릭 영역으로 사용
            if (RectTransformUtility.RectangleContainsScreenPoint(
                rect,
                screenPoint,
                targetCamera))
            {
                return button;
            }
        }

        return null;
    }

    private void DragSlider(Slider slider)
    {
        if (slider == null ||
            !slider.IsInteractable() ||
            !lastHitCanvas ||
            targetCamera == null)
        {
            return;
        }

        // 슬라이더 채우기 영역(없으면 슬라이더 자체) 기준으로 위치 계산
        RectTransform area =
            slider.fillRect != null && slider.fillRect.parent != null
                ? (RectTransform)slider.fillRect.parent
                : (RectTransform)slider.transform;

        Vector2 screenPoint =
            targetCamera.WorldToScreenPoint(lastHitPoint);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                area, screenPoint, targetCamera, out Vector2 local))
        {
            return;
        }

        Rect rect = area.rect;
        bool horizontal =
            slider.direction == Slider.Direction.LeftToRight ||
            slider.direction == Slider.Direction.RightToLeft;

        float t = horizontal
            ? Mathf.InverseLerp(rect.xMin, rect.xMax, local.x)
            : Mathf.InverseLerp(rect.yMin, rect.yMax, local.y);

        if (slider.direction == Slider.Direction.RightToLeft ||
            slider.direction == Slider.Direction.TopToBottom)
        {
            t = 1f - t;
        }

        slider.normalizedValue = t;
    }

    private Slider FindSliderAtWorldPoint(Vector3 worldPoint)
    {
        Vector2 screenPoint =
            targetCamera.WorldToScreenPoint(worldPoint);

        Slider[] sliders =
            targetCanvas.GetComponentsInChildren<Slider>(false);

        foreach (Slider slider in sliders)
        {
            if (!slider.gameObject.activeInHierarchy)
                continue;

            // 음소거 등으로 조작이 막힌 슬라이더는 제외
            if (!slider.IsInteractable())
                continue;

            RectTransform rect =
                slider.GetComponent<RectTransform>();

            if (rect == null)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(
                rect,
                screenPoint,
                targetCamera))
            {
                return slider;
            }
        }

        return null;
    }

    private void UpdateSliderHover(Slider newSlider)
    {
        if (currentSlider == newSlider)
            return;

        if (currentSlider != null && EventSystem.current != null)
        {
            ExecuteEvents.Execute(
                currentSlider.gameObject,
                new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerExitHandler
            );
        }

        currentSlider = newSlider;

        if (currentSlider != null && EventSystem.current != null)
        {
            ExecuteEvents.Execute(
                currentSlider.gameObject,
                new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerEnterHandler
            );
        }
    }

    private void UpdateHover(
        Button newButton)
    {
        if (currentButton == newButton)
            return;

        ClearHover();

        if (newButton == null)
            return;

        currentButton = newButton;

        if (EventSystem.current != null)
        {
            PointerEventData eventData =
                new PointerEventData(
                    EventSystem.current
                );

            ExecuteEvents.Execute(
                currentButton.gameObject,
                eventData,
                ExecuteEvents.pointerEnterHandler
            );
        }

        Debug.Log(
            "UI Hover : " +
            currentButton.name
        );
    }

    private void ClearHover()
    {
        if (currentButton != null &&
            EventSystem.current != null)
        {
            PointerEventData eventData =
                new PointerEventData(
                    EventSystem.current
                );

            ExecuteEvents.Execute(
                currentButton.gameObject,
                eventData,
                ExecuteEvents.pointerExitHandler
            );
        }

        currentButton = null;
    }

    private void PressCurrentButton()
    {
        if (currentButton == null)
        {
            Debug.Log(
                "A 버튼 입력은 감지됐지만 현재 Button이 없습니다."
            );

            return;
        }

        Debug.Log(
            "UI Click : " +
            currentButton.name
        );

        currentButton.onClick.Invoke();
    }

    private void DrawRay(
        Vector3 start,
        Vector3 end)
    {
        if (lineRenderer == null)
            return;

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    private void CreateReticle()
    {
        reticle =
            GameObject.CreatePrimitive(
                PrimitiveType.Sphere
            );

        reticle.name =
            "QuestUIReticle";

        reticle.transform.localScale =
            Vector3.one * reticleSize;

        Collider collider =
            reticle.GetComponent<Collider>();

        if (collider != null)
            Destroy(collider);

        Renderer renderer =
            reticle.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.material.color =
                new Color(
                    0.8f,
                    0.2f,
                    1f
                );
        }

        // 씬이 바뀌어도 레티클이 사라지지 않도록 유지
        DontDestroyOnLoad(reticle);

        reticle.SetActive(false);
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        currentButton = null;
        currentSlider = null;
        draggingSlider = null;

        SetupCanvas(newScene.name);
    }

    private void SetupCanvas(string sceneName)
    {
        if (sceneName == "MainMenuScene")
        {
            GameObject menuObject =
                GameObject.Find("MainMenuCanvas");

            if (menuObject != null)
            {
                mainMenuCanvas =
                    menuObject.GetComponent<Canvas>();

                targetCanvas = mainMenuCanvas;

                // 메인 메뉴에서는 게임 Canvas 숨김
                if (gameCanvas != null)
                    gameCanvas.gameObject.SetActive(false);

                targetCanvas.gameObject.SetActive(true);
            }
        }
        else if (sceneName == "SampleScene")
        {
            // 게임에서는 GameCanvas 활성화
            if (gameCanvas != null)
            {
                gameCanvas.gameObject.SetActive(true);
                targetCanvas = gameCanvas;
            }
        }
        else
        {
            // 그 외 씬(SettingScene 등)은 씬 안의 World Space Canvas 사용
            targetCanvas = FindSceneWorldCanvas(sceneName);
        }

        if (targetCanvas != null)
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (targetCamera != null)
                targetCanvas.worldCamera = targetCamera;

            graphicRaycaster =
                targetCanvas.GetComponent<GraphicRaycaster>();

            Debug.Log(
                "QuestUI Canvas 변경 : " +
                targetCanvas.name
            );
        }
    }

    private static Canvas FindSceneWorldCanvas(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);

        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Canvas canvas = root.GetComponent<Canvas>();

            if (canvas != null &&
                canvas.renderMode == RenderMode.WorldSpace &&
                root.activeInHierarchy)
            {
                return canvas;
            }
        }

        return null;
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;

        if (reticle != null)
            Destroy(reticle);
    }

    // 외부(PauseManager)에서 조준 대상 Canvas를 임시로 바꿀 때 사용
    public void SetTargetCanvas(Canvas canvas)
    {
        // 이전 버튼에 pointerExit를 보내고 상태 초기화
        ClearHover();
        UpdateSliderHover(null);
        draggingSlider = null;

        targetCanvas = canvas;

        if (targetCanvas == null)
            return;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
            targetCanvas.worldCamera = targetCamera;

        graphicRaycaster = targetCanvas.GetComponent<GraphicRaycaster>();
    }

    // 현재 씬의 기본 Canvas(SampleScene이면 GameCanvas)로 복귀
    public void RestoreDefaultCanvas()
    {
        ClearHover();
        UpdateSliderHover(null);
        draggingSlider = null;

        SetupCanvas(SceneManager.GetActiveScene().name);
    }
}
