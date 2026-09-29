using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

/// <summary>
/// 인게임 HUD (새 디자인).
///
/// GameCanvas의 HUD 아래에 실행 시 자동으로 만들어진다. (씬 / 프리팹 수정 없음)
/// - 왼쪽 위 : 현재 거리 / 최고 기록 (Resources/Prefab/Scoreboard.prefab)
/// - 왼쪽 아래 : 바람 세기 게이지 (입김 세기 0~100%)
/// - 가운데 아래 : 시작 전 "A버튼을 누르면 시작합니다" 안내
/// - 정가운데 : 3, 2, 1 카운트다운 (CountdownManager가 숫자를 바꾸고, 여기서 크게 / 튀어나오게 표시)
/// - 예전 HUD 텍스트(SCORE, DISTANCE 등)와 가운데 반투명 흰 판(SafeArea)은 숨긴다.
/// </summary>
public class GameHUD : MonoBehaviour
{
    private const string ScoreboardPath = "Prefab/Scoreboard";
    private const string LocalBestKey = "Local_BestDistance";

    // 배치 (HUD 가운데 기준, Canvas 1단위 = 1mm, 눈앞 1m)
    // 방향 (눈앞 1m 기준 좌표 → 약 왼쪽 18°, 위 12°). 시야 안쪽으로 모으려면 값을 0에 가깝게
    private static readonly Vector2 ScoreboardCenter = new Vector2(-330f, 210f);
    private const float ScoreboardScale = 0.85f;
    private const float WindScale = 1.0f;

    // 점수판 / 바람 게이지 거리 (Canvas 기준 뒤로, 1000 = 1m). 눈에서 거리 = 1m + SideDepth/1000
    // 값이 클수록 멀어지고 그만큼 작게 보인다. (방향은 그대로 왼쪽 위 / 왼쪽 아래)
    // 너무 멀리 두면 방 벽(공간 인식 메시) 뒤로 들어가 가려질 수 있으니 1~2m 정도 권장
    private const float SideDepth = 800f;
    private const float SideDepthFactor = (1000f + SideDepth) / 1000f;

    private static readonly Vector2 WindCenter = new Vector2(-360f, -170f);   // 약 왼쪽 20°, 아래 10°
    private static readonly Vector2 WindSize = new Vector2(84f, 440f);

    private static readonly Vector2 HintCenter = new Vector2(0f, -380f);

    private static readonly Color Primary = Hex("#0C8CE9");
    private static readonly Color IconBg = Hex("#E3F0FB");
    private static readonly Color Track = Hex("#E9ECEF");
    private static readonly Color TextDark = Hex("#1F2328");

    private TMP_FontAsset font;

    private TMP_Text currentDistance;
    private TMP_Text bestDistance;

    private RectTransform windFill;
    private float windTrackHeight;
    private TMP_Text windPercent;
    private float shownWind;

    private CanvasGroup hintGroup;

    private TMP_Text countdownText;
    private string lastCountdown;
    private float countdownPopTime = -10f;

    // ---------- 자동 설치 ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        GameHUDInstaller.EnsureExists();
    }

    /// <summary>
    /// HUD가 실제로 화면에 켜진 뒤(게임씬)에 새 HUD를 만든다.
    /// (꺼져 있는 Canvas 안의 TextMeshPro를 미리 설정하면 오류가 날 수 있어서)
    /// </summary>
    private static HUDController failedHud;

    public static void Install(HUDController hud)
    {
        if (hud == null)
            return;

        if (hud == failedHud || hud.GetComponentInChildren<GameHUD>(true) != null)
            return;

        GameObject root = new GameObject("NewHUD", typeof(RectTransform));
        root.SetActive(false);
        root.transform.SetParent(hud.transform, false);

        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        GameHUD gameHud = root.AddComponent<GameHUD>();

        try
        {
            gameHud.Build(hud);
        }
        catch (System.Exception e)
        {
            // 새 HUD를 못 만들면 예전 HUD를 그대로 사용 (화면에 아무것도 안 보이는 일이 없도록)
            Debug.LogError("[GameHUD] 새 HUD 생성 실패 → 예전 HUD 사용\n" + e);
            failedHud = hud;
            Destroy(root);
            RestoreOldHud(hud);
            return;
        }

        HideOldHud(hud);
        root.SetActive(true);

        Debug.Log("[GameHUD] 새 인게임 HUD 생성");
    }

    /// <summary>
    /// 예전 HUD 텍스트 / 게이지와 가운데 반투명 판 숨기기.
    /// 게임씬에 들어가기 전(HUD가 꺼져 있을 때) 미리 호출해서 예전 HUD가 잠깐이라도 보이지 않게 한다.
    /// </summary>
    public static void HideOldHud(HUDController hud)
    {
        if (hud == null || hud == failedHud)
            return;

        foreach (Transform child in hud.transform)
        {
            if (child.GetComponent<GameHUD>() == null)
                child.gameObject.SetActive(false);
        }

        Transform safeArea = hud.transform.parent;
        if (safeArea != null && safeArea.GetComponent<Canvas>() == null)
        {
            Image safeImage = safeArea.GetComponent<Image>();
            if (safeImage != null)
                safeImage.enabled = false;
        }
    }

    private static void RestoreOldHud(HUDController hud)
    {
        foreach (Transform child in hud.transform)
        {
            if (child.GetComponent<GameHUD>() == null)
                child.gameObject.SetActive(true);
        }
    }

    // ---------- 만들기 ----------

    private void Build(HUDController hud)
    {
        Canvas canvas = hud.GetComponentInParent<Canvas>(true);

        // 가운데 반투명 흰 판(SafeArea 배경) 숨기기 (자식 HUD / 카운트다운은 그대로)
        Transform safeArea = hud.transform.parent;
        if (safeArea != null && safeArea.GetComponent<Canvas>() == null)
        {
            Image safeImage = safeArea.GetComponent<Image>();
            if (safeImage != null)
                safeImage.enabled = false;
        }

        BuildScoreboard();
        BuildWindGauge();
        BuildHint();

        try
        {
            SetupCountdown(canvas);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[GameHUD] 카운트다운 표시 설정 실패 (기존 모양 사용)\n" + e);
        }
    }

    private void BuildScoreboard()
    {
        font = KoreanFont.Get(null);

        GameObject prefab = Resources.Load<GameObject>(ScoreboardPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[GameHUD] Resources/{ScoreboardPath} 프리팹을 찾지 못했습니다.");
            return;
        }

        GameObject board = Instantiate(prefab, transform, false);
        board.name = "Scoreboard";

        RectTransform boardRect = (RectTransform)board.transform;
        boardRect.localScale = Vector3.one * ScoreboardScale;

        // 프리팹 안의 배경(BG) 위치 기준으로 원하는 자리에 배치
        Transform bg = board.transform.Find("BG");
        Vector2 bgOffset = bg != null ? ((RectTransform)bg).anchoredPosition : Vector2.zero;
        // 보이는 방향은 1m 기준 위치와 같게 (거리 배율만큼 바깥으로), 크기는 그대로 → 멀수록 작게 보임
        Vector2 boardPos = ScoreboardCenter * SideDepthFactor - bgOffset * ScoreboardScale;
        boardRect.anchoredPosition3D = new Vector3(boardPos.x, boardPos.y, SideDepth);

        Transform current = board.transform.Find("Distance/DistanceNum");
        Transform best = board.transform.Find("BestDistance/DistanceNum");
        currentDistance = current != null ? current.GetComponent<TMP_Text>() : null;
        bestDistance = best != null ? best.GetComponent<TMP_Text>() : null;

        foreach (TMP_Text text in board.GetComponentsInChildren<TMP_Text>(true))
        {
            text.raycastTarget = false;
            if (font == null)
                font = KoreanFont.Get(text.font);
        }

        foreach (Graphic g in board.GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;
    }

    private void BuildWindGauge()
    {
        // 카드
        Image card = CreateImage("WindGauge", transform, Color.white, RoundedSprites.Rect, 36f);
        Place(card.rectTransform, WindCenter, WindSize);
        card.rectTransform.anchoredPosition3D = new Vector3(
            WindCenter.x * SideDepthFactor, WindCenter.y * SideDepthFactor, SideDepth);
        card.rectTransform.localScale = Vector3.one * WindScale;

        float top = WindSize.y / 2f;

        // 바람 아이콘
        Image iconBg = CreateImage("WindIcon", card.rectTransform, IconBg, RoundedSprites.Circle, 0f);
        Place(iconBg.rectTransform, new Vector2(0f, top - 44f), new Vector2(54f, 54f));

        Image icon = CreateImage("Icon", iconBg.rectTransform, Primary, WindIconSprite.Get(), 0f);
        Place(icon.rectTransform, Vector2.zero, new Vector2(34f, 34f));

        // 트랙
        const float trackTop = 140f;
        const float trackBottom = -150f;
        windTrackHeight = trackTop - trackBottom;

        Image track = CreateImage("Track", card.rectTransform, Track, RoundedSprites.Rect, 18f);
        Place(track.rectTransform, new Vector2(0f, (trackTop + trackBottom) / 2f), new Vector2(36f, windTrackHeight));

        Image fill = CreateImage("Fill", track.rectTransform, Primary, RoundedSprites.Rect, 18f);
        windFill = fill.rectTransform;
        windFill.anchorMin = new Vector2(0f, 0f);
        windFill.anchorMax = new Vector2(1f, 0f);
        windFill.pivot = new Vector2(0.5f, 0f);
        windFill.anchoredPosition = Vector2.zero;
        windFill.sizeDelta = new Vector2(0f, 0f);

        // 퍼센트
        windPercent = CreateText("Percent", card.rectTransform, "0%", 26f, TextDark, FontStyles.Bold);
        Place(windPercent.rectTransform, new Vector2(0f, -top + 34f), new Vector2(WindSize.x, 36f));
    }

    private void BuildHint()
    {
        Image pill = CreateImage("StartHint", transform, new Color(0f, 0f, 0f, 0.5f), RoundedSprites.Rect, 38f);
        Place(pill.rectTransform, HintCenter, new Vector2(600f, 76f));

        TMP_Text text = CreateText("Text", pill.rectTransform, "A버튼을 누르면 시작합니다", 36f, Color.white, FontStyles.Bold);
        Stretch(text.rectTransform);

        hintGroup = pill.gameObject.AddComponent<CanvasGroup>();
        hintGroup.blocksRaycasts = false;
        hintGroup.interactable = false;
    }

    /// <summary>CountdownManager가 쓰는 CountdownText를 크게 가운데에 표시</summary>
    private void SetupCountdown(Canvas canvas)
    {
        if (canvas == null)
            return;

        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name != "CountdownText")
                continue;

            countdownText = text;
            break;
        }

        if (countdownText == null)
            return;

        RectTransform rect = countdownText.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(400f, 320f);

        if (font != null)
            countdownText.font = font;

        countdownText.fontSize = 220f;
        countdownText.fontStyle = FontStyles.Bold;
        countdownText.color = Color.white;
        countdownText.alignment = TextAlignmentOptions.Center;
        countdownText.textWrappingMode = TextWrappingModes.NoWrap;
        countdownText.raycastTarget = false;

        // 테두리는 글자가 처음 화면에 나올 때 적용 (꺼져 있는 TMP에 미리 설정하면 오류가 날 수 있음)
        countdownOutlineApplied = false;
    }

    private bool countdownOutlineApplied;

    /// <summary>패스스루(밝은 배경)에서도 잘 보이도록 카운트다운 숫자에 테두리</summary>
    private void ApplyCountdownOutline()
    {
        if (countdownOutlineApplied)
            return;

        countdownOutlineApplied = true;

        try
        {
            countdownText.outlineWidth = 0.18f;
            countdownText.outlineColor = new Color32(12, 60, 110, 255);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[GameHUD] 카운트다운 테두리 적용 실패\n" + e);
        }
    }

    // ---------- 갱신 ----------

    private void Update()
    {
        GameManager gm = GameManager.Instance;

        // 거리
        float distance = gm != null ? gm.Distance : 0f;

        if (currentDistance != null)
            currentDistance.text = FormatDistance(distance);

        if (bestDistance != null)
            bestDistance.text = FormatDistance(Mathf.Max(GetSavedBest(), distance));

        // 바람 세기 (입김)
        float wind = FlightInputManager.Instance != null ? Mathf.Clamp01(FlightInputManager.Instance.BlowInput) : 0f;
        shownWind = Mathf.Lerp(shownWind, wind, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));

        if (windFill != null)
        {
            windFill.sizeDelta = new Vector2(0f, windTrackHeight * shownWind);
            windFill.gameObject.SetActive(shownWind > 0.01f);
        }

        if (windPercent != null)
            windPercent.text = Mathf.RoundToInt(shownWind * 100f) + "%";

        // 시작 안내
        if (hintGroup != null)
        {
            bool showHint =
                gm != null && !gm.IsPlaying &&
                (CountdownManager.Instance == null || !CountdownManager.Instance.IsCounting) &&
                (ResultUI.Instance == null || !ResultUI.Instance.IsResultShown) &&
                !PauseManager.IsPaused;

            // 은은하게 깜빡임
            float target = showHint ? 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 3f) : 0f;
            hintGroup.alpha = showHint ? target : Mathf.MoveTowards(hintGroup.alpha, 0f, Time.unscaledDeltaTime * 6f);
        }

        // 카운트다운 숫자가 바뀔 때마다 튀어나오는 효과
        if (countdownText != null && countdownText.isActiveAndEnabled)
        {
            ApplyCountdownOutline();

            if (countdownText.text != lastCountdown)
            {
                lastCountdown = countdownText.text;
                countdownPopTime = Time.time;
            }

            float t = Mathf.Clamp01((Time.time - countdownPopTime) / 0.3f);
            float scale = Mathf.Lerp(1.5f, 1f, 1f - (1f - t) * (1f - t));
            countdownText.rectTransform.localScale = Vector3.one * scale;
            countdownText.alpha = Mathf.Lerp(0.2f, 1f, t);
        }
        else
        {
            lastCountdown = null;
        }
    }

    private static float GetSavedBest()
    {
        float best = PlayerPrefs.GetFloat(LocalBestKey, 0f);

        if (SaveManager.Instance != null && SaveManager.Instance.CurrentStats != null)
            best = Mathf.Max(best, SaveManager.Instance.CurrentStats.best_distance);

        return best;
    }

    private static string FormatDistance(float meters)
    {
        return meters >= 100f ? $"{meters:N0} m" : $"{meters:0.0} m";
    }

    // ---------- UI 도우미 ----------

    private static Image CreateImage(string name, Transform parent, Color color, Sprite sprite, float radius)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        img.sprite = sprite;

        if (sprite != null && sprite == RoundedSprites.Rect)
        {
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = radius > 0f ? RoundedSprites.RectRadius / radius : 1f;
        }
        else if (sprite != null)
        {
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
        }

        return img;
    }

    private TMP_Text CreateText(string name, Transform parent, string text, float size, Color color, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void Place(RectTransform rect, Vector2 center, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = center;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}

/// <summary>바람 아이콘 (세 줄 바람 모양) 스프라이트를 실행 시 그려서 사용</summary>
public static class WindIconSprite
{
    private static Sprite sprite;

    public static Sprite Get()
    {
        if (sprite != null)
            return sprite;

        const int size = 128;
        const float unit = size / 24f;          // 24x24 아이콘 좌표 → 픽셀
        const float halfStroke = 1.1f * unit;   // 선 두께 절반

        // 24x24 좌표 (y 아래 방향) 의 선분 목록
        var segments = new System.Collections.Generic.List<Vector4>();

        // 윗줄 : 작은 고리 + 가로선
        AddArc(segments, new Vector2(11f, 6f), 2f, 225f, 450f);
        AddLine(segments, new Vector2(11f, 8f), new Vector2(2f, 8f));

        // 가운데 줄 : 큰 고리 + 긴 가로선
        AddArc(segments, new Vector2(19.5f, 9.5f), 2.5f, 225f, 450f);
        AddLine(segments, new Vector2(19.5f, 12f), new Vector2(2f, 12f));

        // 아랫줄 : 아래로 말린 고리 + 가로선
        AddArc(segments, new Vector2(14f, 18f), 2f, 135f, -90f);
        AddLine(segments, new Vector2(14f, 16f), new Vector2(2f, 16f));

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 텍스처는 y 위 방향 → 아이콘 좌표(y 아래 방향)로 변환
                Vector2 p = new Vector2((x + 0.5f) / unit, (size - y - 0.5f) / unit);

                float d = float.MaxValue;
                foreach (Vector4 s in segments)
                    d = Mathf.Min(d, DistanceToSegment(p, new Vector2(s.x, s.y), new Vector2(s.z, s.w)));

                float a = Mathf.Clamp01(halfStroke - d * unit + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return sprite;
    }

    private static void AddLine(System.Collections.Generic.List<Vector4> list, Vector2 a, Vector2 b)
    {
        list.Add(new Vector4(a.x, a.y, b.x, b.y));
    }

    private static void AddArc(System.Collections.Generic.List<Vector4> list, Vector2 center, float radius, float fromDeg, float toDeg)
    {
        const int steps = 24;
        Vector2 prev = center + radius * new Vector2(Mathf.Cos(fromDeg * Mathf.Deg2Rad), Mathf.Sin(fromDeg * Mathf.Deg2Rad));

        for (int i = 1; i <= steps; i++)
        {
            float deg = Mathf.Lerp(fromDeg, toDeg, i / (float)steps);
            Vector2 next = center + radius * new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));
            AddLine(list, prev, next);
            prev = next;
        }
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f));
        return Vector2.Distance(p, a + ab * t);
    }
}

/// <summary>게임 HUD가 켜지는 순간을 감시해서 새 HUD를 설치 (씬이 바뀌어도 유지)</summary>
public class GameHUDInstaller : MonoBehaviour
{
    private static GameHUDInstaller instance;

    private HUDController hud;
    private float nextCheck;

    public static void EnsureExists()
    {
        if (instance != null)
            return;

        GameObject go = new GameObject("[GameHUD] Installer");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GameHUDInstaller>();
    }

    private void Update()
    {
        if (hud == null)
        {
            if (Time.unscaledTime < nextCheck)
                return;

            nextCheck = Time.unscaledTime + 0.5f;

            hud = FindFirstObjectByType<HUDController>(FindObjectsInactive.Include);
            if (hud == null)
                return;

            // 찾자마자 예전 HUD 숨기기 (아직 꺼져 있어도 안전)
            GameHUD.HideOldHud(hud);
        }

        // HUD가 켜지는 프레임에 바로 새 HUD 생성
        if (hud.isActiveAndEnabled)
            GameHUD.Install(hud);
    }
}
