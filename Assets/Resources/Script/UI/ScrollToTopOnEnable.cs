using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 스크롤 영역이 켜질 때마다 맨 위로 되돌린다.
/// 설정씬의 "일반" 탭(Panel_General)에 자동으로 붙는다.
///
/// 씬 파일에 스크롤이 내려간 상태(Content Y = 263)로 저장되어 있어서
/// 설정씬에 들어가면 "사운드" 제목이 가려진 채로 시작하던 문제를 해결.
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class ScrollToTopOnEnable : MonoBehaviour
{
    private ScrollRect scrollRect;

    // ---------- 설정씬 자동 연결 ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Apply();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Apply();
    }

    private static void Apply()
    {
        SettingsUI ui = Object.FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);

        if (ui == null)
            return;

        foreach (GameObject panel in new[] { ui.panelGeneral, ui.panelSound, ui.panelGraphic })
        {
            if (panel == null)
                continue;

            if (panel.GetComponent<ScrollRect>() != null &&
                panel.GetComponent<ScrollToTopOnEnable>() == null)
            {
                panel.AddComponent<ScrollToTopOnEnable>();
            }
        }
    }

    // ---------- 동작 ----------

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    private void OnEnable()
    {
        ScrollToTop();

        // 줄이 추가되거나 레이아웃이 다시 계산된 뒤에도 한 번 더
        StartCoroutine(ScrollToTopNextFrame());
    }

    private IEnumerator ScrollToTopNextFrame()
    {
        yield return null;
        ScrollToTop();
    }

    private void ScrollToTop()
    {
        if (scrollRect == null || scrollRect.content == null)
            return;

        scrollRect.StopMovement();

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;

        // 콘텐츠 피벗이 위쪽이면 Y = 0 이 맨 위
        RectTransform content = scrollRect.content;
        if (Mathf.Approximately(content.pivot.y, 1f))
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
    }
}
