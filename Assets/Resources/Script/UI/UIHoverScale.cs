using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 포인터(마우스 / VR 레이)가 올라가면 UI를 부드럽게 확대하는 효과.
/// 피벗이 가운데가 아니어도 가운데를 기준으로 커지도록 위치를 보정한다.
/// (부모에 LayoutGroup이 있으면 위치는 레이아웃이 관리하므로 보정하지 않음)
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("호버 시 배율")]
    public float hoverScale = 1.1f;

    [Tooltip("확대/축소 속도")]
    public float speed = 12f;

    private RectTransform rectTransform;
    private Selectable selectable;

    private Vector3 baseScale;
    private Vector2 basePosition;
    private bool compensatePivot;
    private bool initialized;

    private bool isHovering;
    private float currentFactor = 1f;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        if (initialized)
            return;

        rectTransform = (RectTransform)transform;
        selectable = GetComponent<Selectable>();

        baseScale = rectTransform.localScale;
        basePosition = rectTransform.anchoredPosition;

        Transform parent = rectTransform.parent;
        compensatePivot = parent == null || parent.GetComponent<LayoutGroup>() == null;

        initialized = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }

    private void Update()
    {
        // 비활성(음소거 등) 상태에서는 확대하지 않음
        bool canHover = isHovering && (selectable == null || selectable.IsInteractable());

        float target = canHover ? hoverScale : 1f;

        if (Mathf.Approximately(currentFactor, target))
            return;

        currentFactor = Mathf.Lerp(currentFactor, target, Time.unscaledDeltaTime * speed);

        if (Mathf.Abs(currentFactor - target) < 0.001f)
            currentFactor = target;

        Apply();
    }

    private void Apply()
    {
        rectTransform.localScale = baseScale * currentFactor;

        if (compensatePivot)
        {
            // 피벗 기준 확대로 생기는 중심 이동을 되돌림
            Vector2 size = rectTransform.rect.size;
            Vector2 pivotOffset = rectTransform.pivot - new Vector2(0.5f, 0.5f);
            Vector2 scaledOffset = Vector2.Scale(size, pivotOffset);
            scaledOffset = Vector2.Scale(scaledOffset, new Vector2(baseScale.x, baseScale.y)) * (currentFactor - 1f);

            rectTransform.anchoredPosition = basePosition + scaledOffset;
        }
    }

    private void OnDisable()
    {
        // 탭 전환 등으로 꺼질 때 원래 크기로
        if (!initialized)
            return;

        isHovering = false;
        currentFactor = 1f;
        Apply();
    }
}
