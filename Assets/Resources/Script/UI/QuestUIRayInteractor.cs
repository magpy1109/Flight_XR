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

            PressCurrentButton();
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

        reticle.SetActive(false);
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
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
            targetCanvas = null;
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

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;

        if (reticle != null)
            Destroy(reticle);
    }
}