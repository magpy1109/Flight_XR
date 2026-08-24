using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class VRButtonHoverScale : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public Transform playIcon;
    public Transform playText;

    public float hoverScale = 1.1f;
    public float duration = 0.5f;

    private Vector3 iconOriginalScale;
    private Vector3 textOriginalScale;

    private Coroutine iconCoroutine;
    private Coroutine textCoroutine;

    void Start()
    {
        iconOriginalScale = playIcon.localScale;
        textOriginalScale = playText.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (iconCoroutine != null)
            StopCoroutine(iconCoroutine);

        if (textCoroutine != null)
            StopCoroutine(textCoroutine);

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
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (iconCoroutine != null)
            StopCoroutine(iconCoroutine);

        if (textCoroutine != null)
            StopCoroutine(textCoroutine);

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
}