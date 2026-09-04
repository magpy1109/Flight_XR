using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class VRTextHoverColor : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public TMP_Text textComponent;

    public float duration = 0.2f;

    private Coroutine colorCoroutine;

    private Color normalColor;
    private Color hoverColor;

    void Start()
    {
        ColorUtility.TryParseHtmlString("#000000", out normalColor);
        ColorUtility.TryParseHtmlString("#0C8CE9", out hoverColor);

        textComponent.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (colorCoroutine != null)
            StopCoroutine(colorCoroutine);

        colorCoroutine = StartCoroutine(
            ColorAnimation(hoverColor)
        );
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (colorCoroutine != null)
            StopCoroutine(colorCoroutine);

        colorCoroutine = StartCoroutine(
            ColorAnimation(normalColor)
        );
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