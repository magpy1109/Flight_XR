using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 설정씬 "그래픽" 카테고리를 "플레이 설정"으로 바꾸고 줄을 추가한다. (씬 파일 수정 없음)
/// - 탭 이름 : 그래픽 → 플레이
/// - 전체 탭의 "그래픽 설정" 제목 → "플레이 설정"
/// - 플레이 탭 / 전체 탭의 프레임 줄 아래에 추가
///   · 조작 감도 (오른쪽 조이스틱 방향 전환 빠르기) - TurnSensitivity
///   · 야외 모드 켜기 / 끄기 - OutdoorMode
/// 두 탭의 값은 서로 연동된다.
/// </summary>
public static class PlaySettingsSetup
{
    private const string TabName = "플레이";
    private const string HeaderName = "플레이 설정";

    private const string TurnRowName = "Row_TurnSensitivity";
    private const string OutdoorRowName = "Row_OutdoorMode";

    private static readonly List<Slider> turnSliders = new List<Slider>();
    private static readonly List<OnOffSwitch> outdoorSwitches = new List<OnOffSwitch>();
    private static bool syncing;

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

        turnSliders.RemoveAll(s => s == null);
        outdoorSwitches.RemoveAll(s => s == null);

        // 탭 이름 / 제목
        if (ui.graphicText != null)
            ui.graphicText.text = TabName;

        // 줄 템플릿 : 마이크 감도 줄 (슬라이더 줄)
        Transform sliderTemplate = ui.genInputSlider != null ? ui.genInputSlider.transform.parent : null;
        if (sliderTemplate == null)
            return;

        // 전체 탭 : 프레임 줄 아래
        AddRows(ui.genFrameDropdown, sliderTemplate, renameHeader: true);

        // 플레이(그래픽) 탭 : 프레임 줄 아래
        AddRows(ui.graphFrameDropdown, sliderTemplate, renameHeader: false);

        Debug.Log($"[PlaySettingsSetup] 플레이 설정 줄 추가 (조작 감도 {TurnSensitivity.Value:F2}, 야외 모드 {OutdoorMode.Enabled})");
    }

    private static void AddRows(TMP_Dropdown frameDropdown, Transform sliderTemplate, bool renameHeader)
    {
        if (frameDropdown == null)
            return;

        Transform frameRow = frameDropdown.transform.parent;
        Transform panel = frameRow != null ? frameRow.parent : null;
        if (panel == null || panel.Find(TurnRowName) != null)
            return;

        if (renameHeader)
        {
            // 프레임 줄 바로 위의 제목("그래픽 설정")
            int index = frameRow.GetSiblingIndex();
            if (index > 0)
            {
                TMP_Text header = panel.GetChild(index - 1).GetComponent<TMP_Text>();
                if (header != null)
                    header.text = HeaderName;
            }
        }

        // 조작 감도
        GameObject turnRow = CloneRow(sliderTemplate, panel, TurnRowName, frameRow.GetSiblingIndex() + 1,
            "조작 감도", "오른쪽 조이스틱으로 방향을 바꾸는 빠르기");

        Slider slider = turnRow != null ? turnRow.GetComponentInChildren<Slider>(true) : null;
        if (slider != null)
        {
            slider.onValueChanged = new Slider.SliderEvent();
            slider.SetValueWithoutNotify(Mathf.Lerp(slider.minValue, slider.maxValue, TurnSensitivity.Value));
            slider.onValueChanged.AddListener(new UnityAction<float>(_ => OnTurnChanged(slider)));

            if (slider.GetComponent<UIHoverScale>() == null)
                slider.gameObject.AddComponent<UIHoverScale>().hoverScale = 1.05f;

            turnSliders.Add(slider);
        }

        // 야외 모드
        GameObject outdoorRow = CloneRow(sliderTemplate, panel, OutdoorRowName, frameRow.GetSiblingIndex() + 2,
            "야외 모드", "넓은 야외용 : 방 공간 인식 벽을 끄고 주변을 실시간으로 인식해 충돌을 판정합니다");

        if (outdoorRow != null)
        {
            Slider rowSlider = outdoorRow.GetComponentInChildren<Slider>(true);
            RectTransform slot = rowSlider != null ? (RectTransform)rowSlider.transform : null;

            OnOffSwitch sw = OnOffSwitch.Create(outdoorRow.transform, slot, OutdoorMode.Enabled);
            sw.onChanged = OnOutdoorChanged;
            outdoorSwitches.Add(sw);

            if (rowSlider != null)
                Object.Destroy(rowSlider.gameObject);

            // 설명이 길어서 넓게
            Transform desc = outdoorRow.transform.Find("DescriptionText");
            if (desc != null)
            {
                RectTransform rect = (RectTransform)desc;
                rect.sizeDelta = new Vector2(1150f, rect.sizeDelta.y);
            }
        }
    }

    private static GameObject CloneRow(Transform template, Transform panel, string name, int siblingIndex, string title, string description)
    {
        GameObject row = Object.Instantiate(template.gameObject, panel, false);
        row.name = name;
        row.SetActive(true);
        row.transform.SetSiblingIndex(Mathf.Min(siblingIndex, panel.childCount - 1));

        // 마이크 전용 요소(입력 막대, 입김 보정 버튼) 제거
        foreach (MicLevelMeter meter in row.GetComponentsInChildren<MicLevelMeter>(true))
            Object.Destroy(meter.gameObject);
        foreach (MicCalibrationButton button in row.GetComponentsInChildren<MicCalibrationButton>(true))
            Object.Destroy(button.gameObject);

        SetText(row.transform, "TitleText", title);
        SetText(row.transform, "DescriptionText", description);

        // 제목이 잘리지 않게
        Transform titleTransform = row.transform.Find("TitleText");
        if (titleTransform != null)
        {
            RectTransform rect = (RectTransform)titleTransform;
            rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 300f), rect.sizeDelta.y);
        }

        return row;
    }

    private static void OnTurnChanged(Slider source)
    {
        if (syncing)
            return;

        TurnSensitivity.Set(source.normalizedValue);

        syncing = true;
        foreach (Slider other in turnSliders)
        {
            if (other != null && other != source)
                other.SetValueWithoutNotify(Mathf.Lerp(other.minValue, other.maxValue, source.normalizedValue));
        }
        syncing = false;
    }

    private static void OnOutdoorChanged(OnOffSwitch source, bool value)
    {
        if (syncing)
            return;

        OutdoorMode.Set(value);

        syncing = true;
        foreach (OnOffSwitch other in outdoorSwitches)
        {
            if (other != null && other != source)
                other.SetWithoutNotify(value);
        }
        syncing = false;
    }

    private static void SetText(Transform row, string childName, string value)
    {
        Transform child = row.Find(childName);
        if (child == null)
            return;

        TMP_Text text = child.GetComponent<TMP_Text>();
        if (text != null)
            text.text = value;
    }
}

/// <summary>켜기 / 끄기 스위치 (알약 모양 + 동그란 손잡이)</summary>
public class OnOffSwitch : MonoBehaviour
{
    private static readonly Color OnColor = new Color(0.047f, 0.549f, 0.914f, 1f);   // #0C8CE9
    private static readonly Color OffColor = new Color(0.78f, 0.79f, 0.81f, 1f);

    private const float Width = 120f;
    private const float Height = 60f;
    private const float KnobPadding = 6f;

    public System.Action<OnOffSwitch, bool> onChanged;

    public bool IsOn { get; private set; }

    private Image track;
    private RectTransform knob;
    private TMP_Text label;
    private float knobT;

    public static OnOffSwitch Create(Transform row, RectTransform slot, bool isOn)
    {
        GameObject go = new GameObject("OnOffSwitch", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(row, false);

        RectTransform rect = (RectTransform)go.transform;
        if (slot != null)
        {
            // 원래 슬라이더가 있던 오른쪽 위치
            rect.anchorMin = slot.anchorMin;
            rect.anchorMax = slot.anchorMax;
            rect.pivot = slot.pivot;
            rect.anchoredPosition = slot.anchoredPosition;
        }
        else
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = Vector2.zero;
        }
        rect.sizeDelta = new Vector2(Width, Height);

        OnOffSwitch sw = go.AddComponent<OnOffSwitch>();
        sw.Build(isOn);
        return sw;
    }

    private void Build(bool isOn)
    {
        track = GetComponent<Image>();
        track.sprite = RoundedSprites.Rect;
        track.type = Image.Type.Sliced;
        track.pixelsPerUnitMultiplier = RoundedSprites.RectRadius / (Height / 2f);

        Button button = GetComponent<Button>();
        button.targetGraphic = track;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.1f;
        button.colors = colors;
        button.onClick.AddListener(Toggle);

        GameObject knobObject = new GameObject("Knob", typeof(RectTransform), typeof(Image));
        knobObject.transform.SetParent(transform, false);
        knob = (RectTransform)knobObject.transform;
        knob.anchorMin = knob.anchorMax = new Vector2(0f, 0.5f);
        knob.pivot = new Vector2(0.5f, 0.5f);
        float knobSize = Height - KnobPadding * 2f;
        knob.sizeDelta = new Vector2(knobSize, knobSize);

        Image knobImage = knobObject.GetComponent<Image>();
        knobImage.sprite = RoundedSprites.Circle;
        knobImage.color = Color.white;
        knobImage.raycastTarget = false;

        // 스위치 왼쪽에 ON / OFF 글자
        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(transform, false);
        RectTransform labelRect = (RectTransform)labelObject.transform;
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(1f, 0.5f);
        labelRect.anchoredPosition = new Vector2(-14f, 0f);
        labelRect.sizeDelta = new Vector2(90f, Height);

        label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = KoreanFont.Get(label.font);
        label.fontSize = 28f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.MidlineRight;
        label.raycastTarget = false;

        UIHoverScale hover = gameObject.AddComponent<UIHoverScale>();
        hover.hoverScale = 1.08f;

        SetWithoutNotify(isOn);
        knobT = isOn ? 1f : 0f;
        Refresh(knobT);
    }

    public void Toggle()
    {
        SetWithoutNotify(!IsOn);
        onChanged?.Invoke(this, IsOn);
    }

    public void SetWithoutNotify(bool value)
    {
        IsOn = value;

        if (label != null)
        {
            label.text = value ? "ON" : "OFF";
            label.color = value ? OnColor : new Color(0.45f, 0.47f, 0.5f, 1f);
        }
    }

    private void Update()
    {
        float target = IsOn ? 1f : 0f;
        if (Mathf.Approximately(knobT, target))
            return;

        knobT = Mathf.MoveTowards(knobT, target, Time.unscaledDeltaTime * 6f);
        Refresh(knobT);
    }

    private void Refresh(float t)
    {
        if (track != null)
            track.color = Color.Lerp(OffColor, OnColor, t);

        if (knob != null)
        {
            float half = Height / 2f;
            knob.anchoredPosition = new Vector2(Mathf.Lerp(half, Width - half, t), 0f);
        }
    }
}
