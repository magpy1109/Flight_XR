using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

/// <summary>
/// 게임오버 결과 화면. (Resources/Prefab/ResultPanel.prefab 사용)
///
/// - 게임오버 시 ResultUI.ShowResult에서 호출된다.
/// - 일시정지 화면처럼 사용자 정면 1.5m 앞에 고정해서 띄운다. (머리를 따라다니지 않음)
/// - 최종 거리 / 최고 기록 표시, 이번 기록이 최고 기록이면 "신기록!" 표시
/// - 다시도전 → ResultUI.RestartGame / 메인으로 → ResultUI.GoHome
/// - 버튼 호버 시 살짝 커지고 색이 진해짐
///
/// 화면 오브젝트는 게임씬 안에 만들어지므로 씬이 바뀌면 함께 정리된다.
/// </summary>
public class GameOverResultScreen : MonoBehaviour
{
    private const string PrefabPath = "Prefab/ResultPanel";
    private const string LocalBestKey = "Local_BestDistance";

    private const float Distance = 1.5f;
    private const float HeightOffset = -0.1f;
    private const float CanvasScale = 0.001f;

    private static GameOverResultScreen instance;

    private Canvas canvas;
    private TMP_Text presentResult;
    private TMP_Text bestDistance;
    private TMP_Text bestDistanceLabel;
    private string bestLabelDefault;

    private QuestUIRayInteractor legacyRay;

    /// <summary>결과 화면이 떠 있는지</summary>
    public static bool IsShown => instance != null && instance.gameObject.activeSelf;

    // ---------- 표시 / 숨김 ----------

    /// <summary>결과 화면 표시. 프리팹을 못 찾으면 false</summary>
    public static bool Show(float distance)
    {
        GameOverResultScreen screen = GetOrCreate();
        if (screen == null)
            return false;

        screen.ShowInternal(distance);
        return true;
    }

    public static void Hide()
    {
        if (instance == null)
            return;

        instance.HideInternal();
    }

    private static GameOverResultScreen GetOrCreate()
    {
        if (instance != null)
            return instance;

        GameObject prefab = Resources.Load<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[GameOverResultScreen] Resources/{PrefabPath} 프리팹을 찾지 못했습니다.");
            return null;
        }

        // World Space Canvas 생성 (현재 씬 안에)
        GameObject root = new GameObject("ResultCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.SetActive(false);
        root.layer = 5; // UI

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.sizeDelta = new Vector2(640f, 760f); // 결과 카드 크기 (레이 판정 영역)
        rootRect.localScale = Vector3.one * CanvasScale;

        GameObject panel = Instantiate(prefab, rootRect, false);
        panel.name = "ResultPanel";

        GameOverResultScreen screen = root.AddComponent<GameOverResultScreen>();
        screen.canvas = canvas;
        screen.Setup(panel.transform);

        instance = screen;
        return screen;
    }

    private void Setup(Transform panel)
    {
        // 뒤쪽 검은 반투명 배경(DimOverlay)은 사용하지 않음 (패스스루 화면이 가려지지 않게)
        Transform dim = panel.Find("DimOverlay");
        if (dim != null)
            dim.gameObject.SetActive(false);

        presentResult = FindText(panel, "PresentResult");
        bestDistance = FindText(panel, "BestDistance");
        bestDistanceLabel = FindText(panel, "BestDistanceText");
        bestLabelDefault = bestDistanceLabel != null ? bestDistanceLabel.text : "최고 기록";

        SetupButton(panel, "RetryButton", () =>
        {
            if (ResultUI.Instance != null)
                ResultUI.Instance.RestartGame();
        });

        SetupButton(panel, "BackToMainButton", () =>
        {
            if (ResultUI.Instance != null)
                ResultUI.Instance.GoHome();
        });

        // 버튼 클릭음(SoundManager)을 효과음 볼륨 설정에 맞게
        AudioSource click = panel.GetComponentInChildren<AudioSource>(true);
        if (click != null)
        {
            click.ignoreListenerPause = true;
            click.spatialBlend = 0f;

            if (SFXManager.Instance != null && SFXManager.Instance.sfxMixerGroup != null)
                click.outputAudioMixerGroup = SFXManager.Instance.sfxMixerGroup;
        }
    }

    private void ShowInternal(float distance)
    {
        // 최고 기록 (이번 기록 반영 전 값과 비교)
        float previousBest = GetPreviousBest();
        bool newRecord = distance > previousBest && distance > 0f;
        float best = Mathf.Max(previousBest, distance);

        PlayerPrefs.SetFloat(LocalBestKey, best);
        PlayerPrefs.Save();

        if (presentResult != null)
            presentResult.text = FormatDistance(distance);

        if (bestDistance != null)
            bestDistance.text = FormatDistance(best);

        if (bestDistanceLabel != null)
            bestDistanceLabel.text = newRecord ? bestLabelDefault + "  <color=#0C8CE9>신기록!</color>" : bestLabelDefault;

        PlaceInFront();

        gameObject.SetActive(true);

        // 레이 연결
        OfficialRayRunner.RequestRescan();

        if (legacyRay == null)
            legacyRay = FindFirstObjectByType<QuestUIRayInteractor>();
        if (legacyRay != null)
            legacyRay.SetTargetCanvas(canvas);

        Debug.Log($"[GameOverResultScreen] 결과 표시 거리={distance:F1} 최고={best:F1} 신기록={newRecord}");
    }

    private void HideInternal()
    {
        if (!gameObject.activeSelf)
            return;

        if (legacyRay != null)
            legacyRay.RestoreDefaultCanvas();

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    // ---------- 도우미 ----------

    private void PlaceInFront()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        Transform head = cam.transform;

        Vector3 forward = head.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude < 0.001f ? Vector3.forward : forward.normalized;

        Vector3 position = head.position + forward * Distance;
        position.y += HeightOffset;

        transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
        canvas.worldCamera = cam;
    }

    private static float GetPreviousBest()
    {
        float best = PlayerPrefs.GetFloat(LocalBestKey, 0f);

        if (SaveManager.Instance != null && SaveManager.Instance.CurrentStats != null)
            best = Mathf.Max(best, SaveManager.Instance.CurrentStats.best_distance);

        return best;
    }

    private static string FormatDistance(float meters)
    {
        // 짧은 거리는 소수 첫째 자리까지, 100m 이상은 1,234m 형식
        return meters >= 100f ? $"{meters:N0}m" : $"{meters:0.0}m";
    }

    private static TMP_Text FindText(Transform root, string name)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == name)
                return text;
        }

        Debug.LogWarning($"[GameOverResultScreen] ResultPanel에서 {name} 텍스트를 찾지 못했습니다.");
        return null;
    }

    private static void SetupButton(Transform root, string name, UnityEngine.Events.UnityAction onClick)
    {
        Button button = null;

        foreach (Button b in root.GetComponentsInChildren<Button>(true))
        {
            if (b.name == name)
            {
                button = b;
                break;
            }
        }

        if (button == null)
        {
            Debug.LogWarning($"[GameOverResultScreen] ResultPanel에서 {name} 버튼을 찾지 못했습니다.");
            return;
        }

        button.onClick.AddListener(onClick);
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        // 호버 : 색이 진해짐 / 누름 : 더 진해짐
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        // 호버 : 살짝 커짐
        if (button.GetComponent<UIHoverScale>() == null)
        {
            UIHoverScale hover = button.gameObject.AddComponent<UIHoverScale>();
            hover.hoverScale = 1.05f;
        }
    }
}
