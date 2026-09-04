using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class VRButtonHoverScale : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public Transform playIcon;
    public Transform playText;

    public TMP_Text textComponent;

    public float hoverScale = 1.1f;
    public float duration = 0.2f;

    private Vector3 iconOriginalScale;
    private Vector3 textOriginalScale;

    private Coroutine iconCoroutine;
    private Coroutine textCoroutine;
    private Coroutine colorCoroutine;

    private Color normalColor;
    private Color hoverColor;

    void Start()
    {
        iconOriginalScale = playIcon.localScale;
        textOriginalScale = playText.localScale;

        ColorUtility.TryParseHtmlString("#000000", out normalColor);
        ColorUtility.TryParseHtmlString("#0C8CE9", out hoverColor);

        textComponent.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (iconCoroutine != null)
            StopCoroutine(iconCoroutine);

        if (textCoroutine != null)
            StopCoroutine(textCoroutine);

        if (colorCoroutine != null)
            StopCoroutine(colorCoroutine);

        iconCoroutine = StartCoroutine(
            ScaleAnimation(
                playIcon,
                iconOriginalScale * hoverScale
            )
        );

        textCoroutine = StartCoroutine(
            ScaleAnimation(
                playText,
                textOriginalScale * hoverScale
            )
        );

        colorCoroutine = StartCoroutine(
            ColorAnimation(hoverColor)
        );
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (iconCoroutine != null)
            StopCoroutine(iconCoroutine);

        if (textCoroutine != null)
            StopCoroutine(textCoroutine);

        if (colorCoroutine != null)
            StopCoroutine(colorCoroutine);

        iconCoroutine = StartCoroutine(
            ScaleAnimation(
                playIcon,
                iconOriginalScale
            )
        );

        textCoroutine = StartCoroutine(
            ScaleAnimation(
                playText,
                textOriginalScale
            )
        );

        colorCoroutine = StartCoroutine(
            ColorAnimation(normalColor)
        );
    }

    IEnumerator ScaleAnimation(
        Transform target,
        Vector3 targetScale)
    {
        Vector3 startScale = target.localScale;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = time / duration;

            target.localScale = Vector3.Lerp(
                startScale,
                targetScale,
                t
            );

            yield return null;
        }

        target.localScale = targetScale;
    }

    IEnumerator ColorAnimation(Color targetColor)
    {
        Color startColor = textComponent.color;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = time / duration;

            textComponent.color = Color.Lerp(
                startColor,
                targetColor,
                t
            );

            yield return null;
        }

        textComponent.color = targetColor;
    }
}