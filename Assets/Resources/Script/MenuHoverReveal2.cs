using UnityEngine;
using UnityEngine.EventSystems;

public class MenuHoverReveal2 : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private RectTransform icon;
    [SerializeField] private RectTransform text;

    [Header("Movement")]
    [SerializeField] private float moveDistance = 30f;
    [SerializeField] private float moveSpeed = 12f;

    [Header("Text Fade")]
    [SerializeField] private float fadeSpeed = 12f;

    private Vector2 normalIconPosition;
    private Vector2 hoverIconPosition;

    private CanvasGroup textCanvasGroup;

    private bool isHovering;

    private void Awake()
    {
        // 아이콘의 기본 위치 저장
        normalIconPosition = icon.anchoredPosition;

        // Hover 시 아이콘을 왼쪽으로 이동
        hoverIconPosition =
            normalIconPosition + Vector2.left * moveDistance;

        // Text에 CanvasGroup이 없으면 자동으로 추가
        textCanvasGroup = text.GetComponent<CanvasGroup>();

        if (textCanvasGroup == null)
        {
            textCanvasGroup = text.gameObject.AddComponent<CanvasGroup>();
        }

        // 처음에는 텍스트를 숨김
        textCanvasGroup.alpha = 0f;

        // 텍스트는 항상 활성화
        text.gameObject.SetActive(true);
    }

    private void Update()
    {
        // -------------------------
        // 1. Icon 이동
        // -------------------------

        Vector2 targetPosition = isHovering
            ? hoverIconPosition
            : normalIconPosition;

        icon.anchoredPosition = Vector2.Lerp(
            icon.anchoredPosition,
            targetPosition,
            Time.deltaTime * moveSpeed
        );


        // -------------------------
        // 2. Text Fade
        // -------------------------

        float targetAlpha = isHovering ? 1f : 0f;

        textCanvasGroup.alpha = Mathf.Lerp(
            textCanvasGroup.alpha,
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