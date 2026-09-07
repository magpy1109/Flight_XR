using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;

public class QuestUIRayInteractor : MonoBehaviour
{
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
    private bool wasPressed;

    private GameObject reticle;

    private void Start()
    {
        if (targetCanvas == null)
        {
            Debug.LogError("QuestUIRayInteractor: Canvas가 연결되지 않았습니다.");
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogError("QuestUIRayInteractor: Camera를 찾을 수 없습니다.");
            return;
        }

        // World Space Canvas가 어떤 카메라를 사용할지 명확하게 지정
        targetCanvas.worldCamera = targetCamera;

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

        if (!hitCanvas)
        {
            ClearHover();

            if (reticle != null)
                reticle.SetActive(false);

            return;
        }

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

        Button hitButton =
            FindButtonAtWorldPoint(hitPoint);

        UpdateHover(hitButton);

        // QuestInputProvider가 이미 읽고 있는 A 버튼 사용
        if (FlightInputManager.Instance != null &&
            FlightInputManager.Instance.LaunchPressed)
        {
            PressCurrentButton();
        }
    }

    private void GetRightHand()
    {
        if (!rightHand.isValid)
        {
            rightHand =
                InputDevices.GetDeviceAtXRNode(
                    XRNode.RightHand
                );
        }
    }

    private bool TryGetCanvasHit(
        Ray ray,
        out Vector3 hitPoint)
    {
        UnityEngine.Plane canvasPlane =
            new UnityEngine.Plane(
                targetCanvas.transform.forward,
                targetCanvas.transform.position);

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

            // Canvas까지의 거리 체크
            if (distance <= rayLength)
            {
                // Canvas Rect 안에 실제로 들어왔는지 확인
                RectTransform rect =
                    targetCanvas.transform
                        as RectTransform;

                if (rect != null)
                {
                    Vector2 localPoint;

                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
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

    private Button FindButtonAtWorldPoint(
        Vector3 worldPoint)
    {
        Vector2 screenPoint =
            targetCamera.WorldToScreenPoint(
                worldPoint
            );

        Button[] buttons =
            targetCanvas.GetComponentsInChildren<Button>(
                true
            );

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

        PointerEventData eventData =
            new PointerEventData(
                EventSystem.current
            );

        ExecuteEvents.Execute(
            currentButton.gameObject,
            eventData,
            ExecuteEvents.pointerEnterHandler
        );

        Debug.Log(
            "UI Hover : " +
            currentButton.name
        );
    }

    private void ClearHover()
    {
        if (currentButton != null)
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
            return;

        Debug.Log(
            "UI Click : " +
            currentButton.name);

        currentButton.onClick.Invoke();
    }

    // private void ReleaseCurrentButton()
    // {
    //     if (currentButton == null)
    //         return;

    //     PointerEventData eventData =
    //         new PointerEventData(
    //             EventSystem.current
    //         );

    //     ExecuteEvents.Execute(
    //         currentButton.gameObject,
    //         eventData,
    //         ExecuteEvents.pointerUpHandler
    //     );

    //     currentButton.onClick.Invoke();

    //     Debug.Log(
    //         "UI Click : " +
    //         currentButton.name
    //     );
    // }

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
                new Color(0.8f, 0.2f, 1f);
        }

        reticle.SetActive(false);
    }

    private void OnDestroy()
    {
        if (reticle != null)
            Destroy(reticle);
    }
}