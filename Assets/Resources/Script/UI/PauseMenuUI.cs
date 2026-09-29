using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

/// <summary>
/// 일시정지 화면 (새 디자인).
///
/// 게임 씬의 PauseCanvas 안에 실행 시 자동으로 만들어진다. (씬 / 프리팹 수정 없음)
/// - 기존 PausePanel은 숨기고 그대로 둔다. (되돌리려면 이 스크립트만 지우면 됨)
/// - 버튼은 기존 PauseManager 기능을 그대로 호출한다. (계속하기 / 재시도 / 나가기)
/// - 설정 버튼 : 게임을 나가지 않고 음량과 마이크 감도를 바로 조절하는 화면으로 전환
/// - 모든 버튼에 호버 효과 (살짝 커지고 색이 진해짐)
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    // ---------- 디자인 값 (Canvas 1단위 = 1mm) ----------
    private const float CardWidth = 640f;
    private const float CardHeight = 580f;
    private const float Margin = 48f;

    private static readonly Color CardColor = Color.white;
    private static readonly Color PrimaryColor = Hex("#2F7DE1");
    private static readonly Color IconBgColor = Hex("#EAF2FD");
    private static readonly Color TitleColor = Hex("#1F2328");
    private static readonly Color SubColor = Hex("#8A9099");
    private static readonly Color LineColor = Hex("#E3E5E8");
    private static readonly Color SecondaryColor = Hex("#F4F5F7");

    private TMP_FontAsset font;

    private GameObject mainPage;
    private GameObject settingsPage;

    private TMP_Text distanceValue;
    private TMP_Text timeValue;

    private readonly List<SliderBinding> settingSliders =
        new List<SliderBinding>();

    private class SliderBinding
    {
        public Slider slider;
        public System.Func<float> get;
    }

    // ---------- 자동 설치 ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Install();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Install();
    }

    private static void Install()
    {
        PauseManager pauseManager = FindFirstObjectByType<PauseManager>(FindObjectsInactive.Include);
        if (pauseManager == null)
            return;

        Canvas pauseCanvas = GetPauseCanvas(pauseManager);
        if (pauseCanvas == null)
        {
            // 씬에서 PauseCanvas를 지운 경우 : 새로 만들어서 PauseManager에 연결 (B버튼 일시정지가 동작하도록)
            pauseCanvas = CreatePauseCanvas(pauseManager);
            Debug.Log("[PauseMenuUI] PauseCanvas가 없어 새로 만들었습니다.");
        }

        if (pauseCanvas.GetComponentInChildren<PauseMenuUI>(true) != null)
            return;

        // 기존 일시정지 화면 숨기기 (삭제하지 않음)
        foreach (Transform child in pauseCanvas.transform)
            child.gameObject.SetActive(false);

        // 다 만들 때까지 꺼 두기 (호버 효과가 배치가 끝난 위치를 기준으로 잡도록)
        GameObject root = new GameObject("PauseMenu (New)", typeof(RectTransform));
        root.SetActive(false);
        root.transform.SetParent(pauseCanvas.transform, false);

        PauseMenuUI ui = root.AddComponent<PauseMenuUI>();
        ui.Build(pauseCanvas);

        root.SetActive(true);

        Debug.Log("[PauseMenuUI] 새 일시정지 화면 생성");
    }

    private static Canvas CreatePauseCanvas(PauseManager pauseManager)
    {
        GameObject go = new GameObject("PauseCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.SetActive(false);
        go.layer = 5; // UI

        if (pauseManager.gameObject.scene.IsValid())
            SceneManager.MoveGameObjectToScene(go, pauseManager.gameObject.scene);

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;

        RectTransform rect = (RectTransform)go.transform;
        rect.sizeDelta = new Vector2(CardWidth, CardHeight);
        rect.localScale = Vector3.one * 0.001f;

        FieldInfo field = typeof(PauseManager).GetField("pauseCanvas",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field != null)
            field.SetValue(pauseManager, canvas);
        else
            Debug.LogError("[PauseMenuUI] PauseManager.pauseCanvas 필드를 찾지 못했습니다.");

        return canvas;
    }

    private static Canvas GetPauseCanvas(PauseManager pauseManager)
    {
        FieldInfo field = typeof(PauseManager).GetField("pauseCanvas",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        if (field != null && field.GetValue(pauseManager) is Canvas canvas && canvas != null)
            return canvas;

        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c.name == "PauseCanvas")
                return c;
        }

        return null;
    }

    // ---------- 화면 만들기 ----------

    private void Build(Canvas pauseCanvas)
    {
        // 기존 글꼴 사용 (한글 표시)
        TMP_Text existingText = pauseCanvas.GetComponentInChildren<TMP_Text>(true);
        // 한글 글꼴 (PauseCanvas를 새로 만든 경우 기본 글꼴에는 한글이 없어 글자가 깨짐)
        font = KoreanFont.Get(existingText != null ? existingText.font : null);

        // Canvas 크기를 카드에 맞춤 (레이 판정 영역도 같이 맞춰짐)
        RectTransform canvasRect = (RectTransform)pauseCanvas.transform;
        canvasRect.sizeDelta = new Vector2(CardWidth, CardHeight);

        RectTransform root = (RectTransform)transform;
        Stretch(root);

        // 카드 배경
        Image card = CreateImage("Card", root, CardColor, RoundedSprites.Rect, 32f);
        Stretch(card.rectTransform);

        mainPage = CreatePage("MainPage", root);
        settingsPage = CreatePage("SettingsPage", root);

        BuildMainPage((RectTransform)mainPage.transform);
        BuildSettingsPage((RectTransform)settingsPage.transform);

        ShowMain();
    }

    private void BuildMainPage(RectTransform page)
    {
        // 일시정지 아이콘
        Image iconBg = CreateImage("PauseIcon", page, IconBgColor, RoundedSprites.Circle, 0f);
        Place(iconBg.rectTransform, 0f, -95f, 84f, 84f);

        Image barL = CreateImage("BarL", iconBg.rectTransform, PrimaryColor, RoundedSprites.Rect, 3f);
        Place(barL.rectTransform, -8f, 0f, 7f, 26f, center: true);
        Image barR = CreateImage("BarR", iconBg.rectTransform, PrimaryColor, RoundedSprites.Rect, 3f);
        Place(barR.rectTransform, 8f, 0f, 7f, 26f, center: true);

        // 제목 / 설명
        TMP_Text title = CreateText("Title", page, "일시정지", 42f, TitleColor, FontStyles.Bold);
        Place(title.rectTransform, 0f, -178f, CardWidth, 56f);

        TMP_Text sub = CreateText("Subtitle", page, "잠시 멈췄습니다. 이어서 하시겠어요?", 22f, SubColor, FontStyles.Normal);
        Place(sub.rectTransform, 0f, -225f, CardWidth, 32f);

        // 기록 박스 (테두리 + 흰 배경 + 가운데 구분선)
        float boxWidth = CardWidth - Margin * 2f;

        Image boxBorder = CreateImage("StatsBorder", page, LineColor, RoundedSprites.Rect, 16f);
        Place(boxBorder.rectTransform, 0f, -310f, boxWidth, 88f);

        Image box = CreateImage("Stats", boxBorder.rectTransform, CardColor, RoundedSprites.Rect, 15f);
        Stretch(box.rectTransform, 2f);

        Image divider = CreateImage("Divider", box.rectTransform, LineColor, null, 0f);
        Place(divider.rectTransform, 0f, 0f, 2f, 84f, center: true);

        distanceValue = CreateStat(box.rectTransform, "현재 거리", -boxWidth / 4f);
        timeValue = CreateStat(box.rectTransform, "플레이 시간", boxWidth / 4f);

        // 계속하기 (메인 버튼)
        Button resume = CreateButton("ResumeButton", page, "계속하기", true,
            () => { if (PauseManager.Instance != null) PauseManager.Instance.OnClickResume(); });
        Place((RectTransform)resume.transform, 0f, -420f, boxWidth, 68f);

        // 재시도 / 설정 / 나가기
        float gap = 14f;
        float small = (boxWidth - gap * 2f) / 3f;
        float left = -boxWidth / 2f + small / 2f;

        Button retry = CreateButton("RetryButton", page, "재시도", false,
            () => { if (PauseManager.Instance != null) PauseManager.Instance.OnClickRestart(); });
        Place((RectTransform)retry.transform, left, -502f, small, 64f);

        Button settings = CreateButton("SettingsButton", page, "설정", false, ShowSettings);
        Place((RectTransform)settings.transform, left + small + gap, -502f, small, 64f);

        Button exit = CreateButton("ExitButton", page, "나가기", false,
            () => { if (PauseManager.Instance != null) PauseManager.Instance.OnClickExit(); });
        Place((RectTransform)exit.transform, left + (small + gap) * 2f, -502f, small, 64f);
    }

    private TMP_Text CreateStat(RectTransform box, string label, float x)
    {
        TMP_Text labelText = CreateText(label, box, label, 18f, SubColor, FontStyles.Normal);
        Place(labelText.rectTransform, x, 16f, 240f, 26f, center: true);

        TMP_Text value = CreateText(label + "Value", box, "-", 30f, TitleColor, FontStyles.Bold);
        Place(value.rectTransform, x, -16f, 240f, 38f, center: true);

        return value;
    }

    private void BuildSettingsPage(RectTransform page)
    {
        float boxWidth = CardWidth - Margin * 2f;

        TMP_Text title = CreateText("Title", page, "설정", 38f, TitleColor, FontStyles.Bold);
        Place(title.rectTransform, 0f, -64f, CardWidth, 52f);

        TMP_Text sub = CreateText("Subtitle", page, "게임을 나가지 않고 바로 조절할 수 있어요", 20f, SubColor, FontStyles.Normal);
        Place(sub.rectTransform, 0f, -104f, CardWidth, 30f);

        float y = -156f;
        const float step = 54f;

        // 배경음 선택 (전체 랜덤 재생 / 곡 선택 = 그 곡 반복)
        AddBgmSelectRow(page, y);
        y -= step;

        AddSettingRow(page, "전체 음량", y,
            () => AudioSettingsManager.Instance != null ? AudioSettingsManager.Instance.GetSavedMasterVolume() : 1f,
            v => { if (AudioSettingsManager.Instance != null) AudioSettingsManager.Instance.SetMasterVolume(v); });
        y -= step;

        AddSettingRow(page, "배경음", y,
            () => AudioSettingsManager.Instance != null ? AudioSettingsManager.Instance.GetSavedBGMVolume() : 1f,
            v => { if (AudioSettingsManager.Instance != null) AudioSettingsManager.Instance.SetBGMVolume(v); });
        y -= step;

        AddSettingRow(page, "효과음", y,
            () => AudioSettingsManager.Instance != null ? AudioSettingsManager.Instance.GetSavedSFXVolume() : 1f,
            v => { if (AudioSettingsManager.Instance != null) AudioSettingsManager.Instance.SetSFXVolume(v); });
        y -= step;

        AddSettingRow(page, "마이크 감도", y,
            () => MicSensitivity.Value,
            v => MicSensitivity.Set(v));
        y -= step;

        // 오른쪽 조이스틱 방향 전환 빠르기
        AddSettingRow(page, "조작 감도", y,
            () => TurnSensitivity.Value,
            v => TurnSensitivity.Set(v));

        Button back = CreateButton("BackButton", page, "돌아가기", true, ShowMain);
        Place((RectTransform)back.transform, 0f, -502f, boxWidth, 64f);
    }

    private void AddSettingRow(RectTransform page, string label, float y,
        System.Func<float> get, UnityAction<float> set)
    {
        float boxWidth = CardWidth - Margin * 2f;
        float labelWidth = 170f;

        TMP_Text text = CreateText(label, page, label, 24f, TitleColor, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        Place(text.rectTransform, -boxWidth / 2f + labelWidth / 2f, y, labelWidth, 40f);

        float sliderWidth = boxWidth - labelWidth - 10f;
        Slider slider = CreateSlider(label + "Slider", page);
        Place((RectTransform)slider.transform, boxWidth / 2f - sliderWidth / 2f, y, sliderWidth, 40f);

        slider.SetValueWithoutNotify(get());
        slider.onValueChanged.AddListener(set);

        settingSliders.Add(new SliderBinding { slider = slider, get = get });
    }

    // ---------- 배경음 선택 ----------

    private TMP_Text bgmNameText;

    private void AddBgmSelectRow(RectTransform page, float y)
    {
        float boxWidth = CardWidth - Margin * 2f;
        float labelWidth = 170f;

        TMP_Text text = CreateText("배경음 선택", page, "배경음 선택", 24f, TitleColor, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        Place(text.rectTransform, -boxWidth / 2f + labelWidth / 2f, y, labelWidth, 40f);

        float areaWidth = boxWidth - labelWidth - 10f;
        float areaCenter = boxWidth / 2f - areaWidth / 2f;
        const float arrowSize = 48f;

        Button prev = CreateButton("BgmPrevButton", page, "<", false, () => ChangeBgm(-1));
        Place((RectTransform)prev.transform, areaCenter - areaWidth / 2f + arrowSize / 2f, y, arrowSize, arrowSize);

        Button next = CreateButton("BgmNextButton", page, ">", false, () => ChangeBgm(1));
        Place((RectTransform)next.transform, areaCenter + areaWidth / 2f - arrowSize / 2f, y, arrowSize, arrowSize);

        bgmNameText = CreateText("BgmName", page, "-", 22f, TitleColor, FontStyles.Normal);
        bgmNameText.overflowMode = TextOverflowModes.Ellipsis;
        Place(bgmNameText.rectTransform, areaCenter, y, areaWidth - arrowSize * 2f - 16f, 40f);

        RefreshBgmName();
    }

    /// <summary>
    /// 선택 순서 : 전체 랜덤 재생 → 1번 곡 → 2번 곡 … → 다시 전체 랜덤 재생
    /// (설정씬의 BGM 선택과 같은 값을 사용하고 바로 적용된다)
    /// </summary>
    private void ChangeBgm(int direction)
    {
        BGMManager bgm = BGMManager.Instance;
        if (bgm == null || bgm.generalBGMList == null || bgm.generalBGMList.Count == 0)
            return;

        int count = bgm.generalBGMList.Count + 1;            // 0 = 전체 랜덤, 1.. = 곡
        int current = bgm.SavedGeneralSelection < 0 ? 0 : bgm.SavedGeneralSelection + 1;
        int nextValue = ((current + direction) % count + count) % count;

        bgm.SelectGeneralBGM(nextValue == 0 ? BGMManager.SelectAllShuffle : nextValue - 1);
        RefreshBgmName();
    }

    private void RefreshBgmName()
    {
        if (bgmNameText == null)
            return;

        BGMManager bgm = BGMManager.Instance;
        if (bgm == null || bgm.generalBGMList == null || bgm.generalBGMList.Count == 0)
        {
            bgmNameText.text = "-";
            return;
        }

        int selection = bgm.SavedGeneralSelection;

        if (selection < 0)
        {
            bgmNameText.text = "전체 랜덤 재생";
            return;
        }

        AudioClip clip = bgm.generalBGMList[selection];
        bgmNameText.text = clip != null ? clip.name : "(비어 있음)";
    }

    // ---------- 페이지 전환 ----------

    private void ShowMain()
    {
        mainPage.SetActive(true);
        settingsPage.SetActive(false);
        RefreshStats();
    }

    private void ShowSettings()
    {
        // 다른 곳(설정씬)에서 바꾼 값이 있을 수 있으므로 다시 읽기
        foreach (var entry in settingSliders)
            entry.slider.SetValueWithoutNotify(entry.get());

        RefreshBgmName();

        mainPage.SetActive(false);
        settingsPage.SetActive(true);
    }

    private void OnEnable()
    {
        // 일시정지할 때마다 첫 화면 + 최신 기록으로
        if (mainPage != null)
            ShowMain();

        HideHud();
    }

    private void OnDisable()
    {
        RestoreHud();
    }

    // ---------- 게임 HUD 숨기기 ----------
    // 거리 / 높이 등 HUD(GameCanvas)는 눈앞 1m에 붙어 있어서 1.5m 앞의 일시정지 화면보다 앞에 보인다.
    // 일시정지 중에는 HUD Canvas 표시만 끄고(스크립트는 그대로 동작), 재개하면 다시 켠다.

    private Canvas hiddenHudCanvas;

    private void HideHud()
    {
        HUDController hud = FindFirstObjectByType<HUDController>(FindObjectsInactive.Exclude);
        if (hud == null)
            return;

        Canvas hudCanvas = hud.GetComponentInParent<Canvas>();
        if (hudCanvas == null)
            return;

        hudCanvas = hudCanvas.rootCanvas;

        Canvas ownCanvas = GetComponentInParent<Canvas>(true);
        if (ownCanvas != null && hudCanvas == ownCanvas.rootCanvas)
            return;

        if (hudCanvas.enabled)
        {
            hudCanvas.enabled = false;
            hiddenHudCanvas = hudCanvas;
        }
    }

    private void RestoreHud()
    {
        if (hiddenHudCanvas != null)
            hiddenHudCanvas.enabled = true;

        hiddenHudCanvas = null;
    }

    // ---------- 기록 표시 ----------

    private static FieldInfo startTimeField;

    private void RefreshStats()
    {
        GameManager gm = GameManager.Instance;

        if (distanceValue != null)
            distanceValue.text = gm != null ? $"{gm.Distance:N0}m" : "-";

        if (timeValue != null)
        {
            float seconds = 0f;

            if (gm != null && gm.IsPlaying)
            {
                if (startTimeField == null)
                    startTimeField = typeof(GameManager).GetField("startTime", BindingFlags.Instance | BindingFlags.NonPublic);

                if (startTimeField != null)
                    seconds = Mathf.Max(0f, Time.time - (float)startTimeField.GetValue(gm));
            }

            int total = Mathf.FloorToInt(seconds);
            timeValue.text = $"{total / 60:00}:{total % 60:00}";
        }
    }

    // ---------- UI 생성 도우미 ----------

    private static GameObject CreatePage(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform);
        return go;
    }

    private static Image CreateImage(string name, RectTransform parent, Color color, Sprite sprite, float radius)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        if (sprite != null)
        {
            img.sprite = sprite;

            if (sprite == RoundedSprites.Rect)
            {
                img.type = Image.Type.Sliced;
                // 스프라이트 모서리(32px)를 원하는 반지름으로
                img.pixelsPerUnitMultiplier = radius > 0f ? RoundedSprites.RectRadius / radius : 1f;
            }
            else
            {
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
            }
        }

        return img;
    }

    private TMP_Text CreateText(string name, RectTransform parent, string text, float size, Color color, FontStyles style)
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

    private Button CreateButton(string name, RectTransform parent, string label, bool primary, UnityAction onClick)
    {
        Image bg;

        if (primary)
        {
            bg = CreateImage(name, parent, PrimaryColor, RoundedSprites.Rect, 18f);
        }
        else
        {
            // 연한 회색 버튼 + 얇은 테두리
            bg = CreateImage(name, parent, LineColor, RoundedSprites.Rect, 18f);
            Image inner = CreateImage("Fill", bg.rectTransform, SecondaryColor, RoundedSprites.Rect, 17f);
            Stretch(inner.rectTransform, 2f);
        }

        bg.raycastTarget = true;

        Button button = bg.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        // 호버 : 색이 진해짐 / 누름 : 더 진해짐
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = primary ? new Color(0.82f, 0.82f, 0.82f, 1f) : new Color(0.88f, 0.88f, 0.88f, 1f);
        colors.pressedColor = primary ? new Color(0.68f, 0.68f, 0.68f, 1f) : new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        if (!primary)
        {
            // 안쪽 회색 면도 같이 진해지도록
            Image inner = bg.transform.Find("Fill").GetComponent<Image>();
            button.gameObject.AddComponent<ButtonTintFollower>().Setup(button, inner);
        }

        TMP_Text text = CreateText("Label", bg.rectTransform, label, primary ? 28f : 26f,
            primary ? Color.white : TitleColor, FontStyles.Bold);
        Stretch(text.rectTransform);

        button.onClick.AddListener(onClick);

        // 호버 시 살짝 커짐
        UIHoverScale hover = button.gameObject.AddComponent<UIHoverScale>();
        hover.hoverScale = 1.04f;

        return button;
    }

    private Slider CreateSlider(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        // 배경 트랙
        Image track = CreateImage("Background", (RectTransform)go.transform, LineColor, RoundedSprites.Rect, 6f);
        RectTransform trackRect = track.rectTransform;
        trackRect.anchorMin = new Vector2(0f, 0.5f);
        trackRect.anchorMax = new Vector2(1f, 0.5f);
        trackRect.sizeDelta = new Vector2(0f, 12f);
        trackRect.anchoredPosition = Vector2.zero;
        track.raycastTarget = true; // 트랙 아무 곳이나 눌러도 이동

        // 채워지는 부분
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        RectTransform fillAreaRect = (RectTransform)fillArea.transform;
        fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRect.sizeDelta = new Vector2(0f, 12f);

        Image fill = CreateImage("Fill", fillAreaRect, PrimaryColor, RoundedSprites.Rect, 6f);
        fill.rectTransform.sizeDelta = Vector2.zero;

        // 손잡이
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        RectTransform handleAreaRect = (RectTransform)handleArea.transform;
        Stretch(handleAreaRect);
        handleAreaRect.offsetMin = new Vector2(14f, 0f);
        handleAreaRect.offsetMax = new Vector2(-14f, 0f);

        Image handleBorder = CreateImage("Handle", handleAreaRect, PrimaryColor, RoundedSprites.Circle, 0f);
        handleBorder.rectTransform.sizeDelta = new Vector2(30f, 30f);
        handleBorder.raycastTarget = true;

        Image handleInner = CreateImage("Inner", handleBorder.rectTransform, Color.white, RoundedSprites.Circle, 0f);
        Stretch(handleInner.rectTransform, 5f);

        Slider slider = go.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handleBorder.rectTransform;
        slider.targetGraphic = handleBorder;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };

        UIHoverScale hover = go.AddComponent<UIHoverScale>();
        hover.hoverScale = 1.03f;

        return slider;
    }

    private static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    /// <summary>카드 위쪽 가운데 기준(center=false) 또는 부모 가운데 기준(center=true)으로 배치</summary>
    private static void Place(RectTransform rect, float x, float y, float width, float height, bool center = false)
    {
        rect.anchorMin = rect.anchorMax = center ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}

/// <summary>회색 버튼의 안쪽 면도 버튼 호버/누름 색을 따라가게 함</summary>
public class ButtonTintFollower : MonoBehaviour
{
    private Button button;
    private Image target;

    public void Setup(Button source, Image follower)
    {
        button = source;
        target = follower;
    }

    private void LateUpdate()
    {
        if (button == null || target == null || button.targetGraphic == null)
            return;

        target.canvasRenderer.SetColor(button.targetGraphic.canvasRenderer.GetColor());
    }
}

/// <summary>둥근 사각형 / 원 스프라이트를 실행 시 만들어 공유</summary>
public static class RoundedSprites
{
    public const float RectRadius = 32f;

    private static Sprite rect;
    private static Sprite circle;

    public static Sprite Rect => rect != null ? rect : (rect = CreateRoundedRect((int)RectRadius));
    public static Sprite Circle => circle != null ? circle : (circle = CreateCircle(128));

    private static Sprite CreateRoundedRect(int radius)
    {
        int size = radius * 2 + 4;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float r = radius;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 가장 가까운 모서리 원 중심까지 거리로 가장자리 부드럽게
                float cx = Mathf.Clamp(x + 0.5f, r, size - r);
                float cy = Mathf.Clamp(y + 0.5f, r, size - r);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                float a = Mathf.Clamp01(r - d + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();

        float border = radius + 1;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }

    private static Sprite CreateCircle(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float r = size / 2f;
        Vector2 c = new Vector2(r, r);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                float a = Mathf.Clamp01(r - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}

/// <summary>한글이 들어 있는 TextMeshPro 글꼴 (Resources/Font/NanumGothicBold SDF)</summary>
public static class KoreanFont
{
    private const string Path = "Font/NanumGothicBold SDF";

    private static TMP_FontAsset font;
    private static bool loaded;

    /// <summary>나눔고딕을 찾지 못하면 fallback, 그것도 없으면 TMP 기본 글꼴</summary>
    public static TMP_FontAsset Get(TMP_FontAsset fallback)
    {
        if (!loaded)
        {
            loaded = true;
            font = Resources.Load<TMP_FontAsset>(Path);

            if (font == null)
                Debug.LogWarning($"[KoreanFont] Resources/{Path} 글꼴을 찾지 못했습니다.");
        }

        if (font != null)
            return font;

        return fallback != null ? fallback : TMP_Settings.defaultFontAsset;
    }
}
