using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FadeManager : MonoBehaviour
{
    public CanvasGroup fadePanel;
    public float fadeDuration = 1f;

    private void Start()
    {
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        float t = 1f;

        while (t > 0)
        {
            t -= Time.deltaTime / fadeDuration;
            fadePanel.alpha = t;
            yield return null;
        }

        fadePanel.alpha = 0;
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(FadeOut(sceneName));
    }

    IEnumerator FadeOut(string sceneName)
    {
        float t = 0f;

        while (t < 1)
        {
            t += Time.deltaTime / fadeDuration;
            fadePanel.alpha = t;
            yield return null;
        }

        SceneManager.LoadScene(sceneName);
    }
}