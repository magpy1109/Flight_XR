using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuHoverReveal : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private GameObject icon;
    [SerializeField] private RectTransform text;

    [Header("Movement")]
    [SerializeField] private float moveDistance = 30f;
    [SerializeField] private float moveSpeed = 12f;

    [Header("Icon Fade")]
    [SerializeField] private bool useFade = true;
    [SerializeField] private float fadeSpeed = 12f;

    private Vector2 normalPosition;
    private Vector2 hoverPosition;

    private CanvasGroup iconCanvasGroup;
    private bool isHovering;

    private void Awake()
    {
        // Text의 기본 위치 저장
        normalPosition = text.anchoredPosition;

        // Hover 시 오른쪽으로 이동할 위치
        hoverPosition = normalPosition + Vector2.right * moveDistance;

        // Icon에 CanvasGroup이 없으면 자동으로 추가
        iconCanvasGroup = icon.GetComponent<CanvasGroup>();

        if (iconCanvasGroup == null)
        {
            iconCanvasGroup = icon.AddComponent<CanvasGroup>();
        }

        // 처음에는 아이콘을 숨김
        iconCanvasGroup.alpha = 0f;
        icon.SetActive(true);
    }

    private void Update()
    {
        // -------------------------
        // 1. Text 이동
        // -------------------------

        Vector2 targetPosition = isHovering
            ? hoverPosition
            : normalPosition;

        text.anchoredPosition = Vector2.Lerp(
            text.anchoredPosition,
            targetPosition,
            Time.deltaTime * moveSpeed
        );


        // -------------------------
        // 2. Icon Fade
        // -------------------------

        float targetAlpha = isHovering ? 1f : 0f;

        iconCanvasGroup.alpha = Mathf.Lerp(
            iconCanvasGroup.alpha,
            targetAlpha,
            Time.deltaTime * fadeSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }
}