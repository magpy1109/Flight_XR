using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    // 추가
    public FadeManager fadeManager;

    [Header("[ Tab Buttons ]")]
    public Button tabGeneralBtn;
    public Button tabSoundBtn;
    public Button tabGraphicBtn;

    [Header("[ Select Borders ]")]
    public GameObject generalBorder;
    public GameObject soundBorder;
    public GameObject graphicBorder;

    [Header("[ Tab Text & Icons ]")]
    public TextMeshProUGUI generalText;
    public TextMeshProUGUI soundText;
    public Image soundIcon;
    public TextMeshProUGUI graphicText;
    public Image graphicIcon;

    [Header("[ Icon Sprites ]")]
    public Sprite soundDefaultSprite;
    public Sprite soundActiveSprite;
    public Sprite graphicDefaultSprite;
    public Sprite graphicActiveSprite;

    [Header("[ Content Panels ]")]
    public GameObject panelGeneral;
    public GameObject panelSound;
    public GameObject panelGraphic;

    [Header("[ General Tab UI Elements ]")]
    public Slider genInputSlider;
    public Slider genMasterSlider;
    public Slider genBGMSlider;
    public Slider genSFXSlider;
    public TMP_Dropdown genFrameDropdown;

    [Header("[ Individual Tab UI Elements ]")]
    public Slider soundInputSlider;
    public Slider soundMasterSlider;
    public Slider soundBGMSlider;
    public Slider soundSFXSlider;
    public TMP_Dropdown graphFrameDropdown;

    [Header("[ Mute Row Sync ]")] // ★ [새로 추가] 각 줄의 음소거 컴포넌트 연동 칸
    public SoundRowMuter genMasterMuter;
    public SoundRowMuter soundMasterMuter;
    public SoundRowMuter genBGMMuter;
    public SoundRowMuter soundBGMMuter;
    public SoundRowMuter genSFXMuter;
    public SoundRowMuter soundSFXMuter;

    [Header("[ BGM 선택 / 반복 재생 줄 (실행 시 자동 생성) ]")]
    public string bgmSelectTitle = "배경음악 선택";
    public string bgmSelectDescriptionText = "곡을 고르면 그 곡만 반복 재생됩니다";
    public string bgmShuffleAllText = "전체 랜덤 재생";

    private readonly List<TMP_Dropdown> bgmSelectDropdowns = new List<TMP_Dropdown>();

    private bool isSyncing = false;
    private Color activeTextColor;
    private Color inactiveTextColor;

    // 슬라이더 Min/Max가 0~100이면 100f, 0~1이면 1f로 설정
    private const float SLIDER_SCALE = 100f;

    void Start()
    {
        ColorUtility.TryParseHtmlString("#0C8CE9", out activeTextColor);
        ColorUtility.TryParseHtmlString("#1A1A1A", out inactiveTextColor);

        tabGeneralBtn.onClick.AddListener(() => SwitchTab(0));
        tabSoundBtn.onClick.AddListener(() => SwitchTab(1));
        tabGraphicBtn.onClick.AddListener(() => SwitchTab(2));

        // 프레임 드롭다운 옵션을 실제 적용되는 주사율 목록으로 채움
        SetupFrameDropdownOptions();

        // 사운드 탭 / 일반 탭에 BGM 선택 + 반복 재생 줄 생성
        BuildBgmRows();

        // 슬라이더 값 동기화 (General 탭 <-> Sound 탭)
        SetupSliderSync(genInputSlider, soundInputSlider);
        SetupSliderSync(genMasterSlider, soundMasterSlider);
        SetupSliderSync(genBGMSlider, soundBGMSlider);
        SetupSliderSync(genSFXSlider, soundSFXSlider);

        // 드롭다운 값 동기화 (프레임만 남음 - 해상도/화면크기 제거됨)
        SetupDropdownSync(genFrameDropdown, graphFrameDropdown);

        // ★ 줄 음소거 거울 동기화 연결
        SetupMuteSync(genMasterMuter, soundMasterMuter);
        SetupMuteSync(genBGMMuter, soundBGMMuter);
        SetupMuteSync(genSFXMuter, soundSFXMuter);

        // ★★ [새로 추가] 저장된 설정값을 UI에 먼저 반영
        LoadSavedValuesToUI();

        // ★★ [새로 추가] 실제 오디오/프레임레이트 매니저에 값을 적용하는 리스너 연결
        BindRealSettingsListeners();

        SwitchTab(0);
    }

    // ★★ [새로 추가] PlayerPrefs에 저장돼 있던 값을 슬라이더/드롭다운/음소거 버튼에 반영
    private void LoadSavedValuesToUI()
    {
        if (AudioSettingsManager.Instance != null)
        {
            // 슬라이더 Max Value가 100이면 SLIDER_SCALE = 100f, 슬라이더가 0~1 범위면 1f로 바꿔주세요.
            float master = AudioSettingsManager.Instance.GetSavedMasterVolume() * SLIDER_SCALE;
            float bgm = AudioSettingsManager.Instance.GetSavedBGMVolume() * SLIDER_SCALE;
            float sfx = AudioSettingsManager.Instance.GetSavedSFXVolume() * SLIDER_SCALE;

            genMasterSlider.SetValueWithoutNotify(master);
            genBGMSlider.SetValueWithoutNotify(bgm);
            genSFXSlider.SetValueWithoutNotify(sfx);

            soundMasterSlider.SetValueWithoutNotify(master);
            soundBGMSlider.SetValueWithoutNotify(bgm);
            soundSFXSlider.SetValueWithoutNotify(sfx);

            if (genMasterMuter != null) genMasterMuter.SetMute(AudioSettingsManager.Instance.GetSavedMasterMute());
            if (genBGMMuter != null) genBGMMuter.SetMute(AudioSettingsManager.Instance.GetSavedBGMMute());
            if (genSFXMuter != null) genSFXMuter.SetMute(AudioSettingsManager.Instance.GetSavedSFXMute());
        }

        if (FrameRateSettingsManager.Instance != null)
        {
            int savedIndex = FrameRateSettingsManager.Instance.GetSavedIndex();
            genFrameDropdown.SetValueWithoutNotify(savedIndex);
            graphFrameDropdown.SetValueWithoutNotify(savedIndex);
        }
    }

    // ★★ [새로 추가] UI 조작이 실제 설정에 반영되도록 매니저 함수들을 연결
    private void BindRealSettingsListeners()
    {
        if (AudioSettingsManager.Instance != null)
        {
            // 슬라이더 값(0~100 또는 0~1)을 매니저가 기대하는 0~1 값으로 나눠서 전달
            genMasterSlider.onValueChanged.AddListener((val) => AudioSettingsManager.Instance.SetMasterVolume(val / SLIDER_SCALE));
            genBGMSlider.onValueChanged.AddListener((val) => AudioSettingsManager.Instance.SetBGMVolume(val / SLIDER_SCALE));
            genSFXSlider.onValueChanged.AddListener((val) => AudioSettingsManager.Instance.SetSFXVolume(val / SLIDER_SCALE));

            if (genMasterMuter != null) genMasterMuter.onMuteChanged += AudioSettingsManager.Instance.SetMasterMute;
            if (genBGMMuter != null) genBGMMuter.onMuteChanged += AudioSettingsManager.Instance.SetBGMMute;
            if (genSFXMuter != null) genSFXMuter.onMuteChanged += AudioSettingsManager.Instance.SetSFXMute;
        }
        else
        {
            Debug.LogWarning("[SettingsUI] AudioSettingsManager 인스턴스가 없습니다. 씬에 배치했는지 확인하세요.");
        }

        if (FrameRateSettingsManager.Instance != null)
        {
            genFrameDropdown.onValueChanged.AddListener(FrameRateSettingsManager.Instance.ApplyFrameRate);
        }
        else
        {
            Debug.LogWarning("[SettingsUI] FrameRateSettingsManager 인스턴스가 없습니다. 씬에 배치했는지 확인하세요.");
        }
    }

    public void SwitchTab(int tabIndex)
    {
        generalBorder.SetActive(false);
        soundBorder.SetActive(false);
        graphicBorder.SetActive(false);

        switch (tabIndex)
        {
            case 0: generalBorder.SetActive(true); break;
            case 1: soundBorder.SetActive(true); break;
            case 2: graphicBorder.SetActive(true); break;
        }

        if (generalText != null) generalText.color = inactiveTextColor;
        if (soundText != null) soundText.color = inactiveTextColor;
        if (graphicText != null) graphicText.color = inactiveTextColor;

        if (soundIcon != null && soundDefaultSprite != null) soundIcon.sprite = soundDefaultSprite;
        if (graphicIcon != null && graphicDefaultSprite != null) graphicIcon.sprite = graphicDefaultSprite;

        if (soundIcon != null) soundIcon.color = Color.white;
        if (graphicIcon != null) graphicIcon.color = Color.white;

        switch (tabIndex)
        {
            case 0:
                if (generalText != null) generalText.color = activeTextColor;
                break;
            case 1:
                if (soundText != null) soundText.color = activeTextColor;
                if (soundIcon != null && soundActiveSprite != null) soundIcon.sprite = soundActiveSprite;
                break;
            case 2:
                if (graphicText != null) graphicText.color = activeTextColor;
                if (graphicIcon != null && graphicActiveSprite != null) graphicIcon.sprite = graphicActiveSprite;
                break;
        }

        panelGeneral.SetActive(false);
        panelSound.SetActive(false);
        panelGraphic.SetActive(false);

        switch (tabIndex)
        {
            case 0: panelGeneral.SetActive(true); break;
            case 1: panelSound.SetActive(true); break;
            case 2: panelGraphic.SetActive(true); break;
        }
    }

    // ★ [새로 추가] 음소거 상태를 실시간 거울 동기화 시켜주는 함수
    private void SetupMuteSync(SoundRowMuter a, SoundRowMuter b)
    {
        if (a == null || b == null) return;

        a.onMuteChanged += (muteState) =>
        {
            if (isSyncing) return;
            isSyncing = true;
            b.SetMute(muteState);
            isSyncing = false;
        };

        b.onMuteChanged += (muteState) =>
        {
            if (isSyncing) return;
            isSyncing = true;
            a.SetMute(muteState);
            isSyncing = false;
        };
    }

    private void SetupSliderSync(Slider a, Slider b)
    {
        if (a == null || b == null) return;
        a.onValueChanged.AddListener((val) => { if (isSyncing) return; isSyncing = true; b.value = val; isSyncing = false; });
        b.onValueChanged.AddListener((val) => { if (isSyncing) return; isSyncing = true; a.value = val; isSyncing = false; });
    }

    private void SetupDropdownSync(TMP_Dropdown a, TMP_Dropdown b)
    {
        if (a == null || b == null) return;
        a.onValueChanged.AddListener((val) => { if (isSyncing) return; isSyncing = true; b.value = val; isSyncing = false; });
        b.onValueChanged.AddListener((val) => { if (isSyncing) return; isSyncing = true; a.value = val; isSyncing = false; });
    }

    // ------------------------------------------------
    // 프레임 드롭다운 옵션
    // ------------------------------------------------

    // 씬에 입력된 옵션 문구(60/144/240/제한 없음)는 실제 적용 값(72/80/90/120Hz)과 달라서
    // 매니저의 목록으로 덮어쓴다. (순서가 항상 frameRateOptions와 같아짐)
    private void SetupFrameDropdownOptions()
    {
        if (FrameRateSettingsManager.Instance == null)
            return;

        List<string> labels = FrameRateSettingsManager.Instance.GetOptionLabels();

        foreach (TMP_Dropdown dropdown in new[] { genFrameDropdown, graphFrameDropdown })
        {
            if (dropdown == null) continue;

            dropdown.ClearOptions();
            dropdown.AddOptions(labels);
        }
    }

    // ------------------------------------------------
    // BGM 선택 / 반복 재생
    // ------------------------------------------------

    private void BuildBgmRows()
    {
        if (BGMManager.Instance == null)
        {
            Debug.LogWarning("[SettingsUI] BGMManager 인스턴스가 없어 BGM 선택 줄을 만들지 않습니다.");
            return;
        }

        if (graphFrameDropdown == null)
        {
            Debug.LogWarning("[SettingsUI] 복제할 드롭다운(graphFrameDropdown)이 없어 BGM 선택 줄을 만들지 않습니다.");
            return;
        }

        // 그래픽 탭의 "프레임" 줄(드롭다운이 있는 줄)을 복제해서 사용
        Transform template = graphFrameDropdown.transform.parent;

        CreateBgmRow(template, soundBGMSlider);
        CreateBgmRow(template, genBGMSlider);

        // 곡 목록 : 0 = 전체 랜덤 재생(기본), 1 이상 = 곡
        List<string> songOptions = new List<string> { bgmShuffleAllText };
        songOptions.AddRange(BGMManager.Instance.GetGeneralBGMNames());

        int songValue = SelectionToDropdown(BGMManager.Instance.SavedGeneralSelection);

        foreach (TMP_Dropdown dropdown in bgmSelectDropdowns)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(songOptions);
            dropdown.SetValueWithoutNotify(songValue);
            dropdown.RefreshShownValue();

            dropdown.onValueChanged.AddListener(value =>
            {
                if (isSyncing) return;
                isSyncing = true;
                foreach (TMP_Dropdown other in bgmSelectDropdowns)
                    if (other != dropdown) other.SetValueWithoutNotify(value);
                isSyncing = false;

                if (BGMManager.Instance != null)
                    BGMManager.Instance.SelectGeneralBGM(DropdownToSelection(value));
            });
        }
    }

    private static int SelectionToDropdown(int selection)
    {
        return selection >= 0 ? selection + 1 : 0; // 0 = 전체 랜덤 재생
    }

    private static int DropdownToSelection(int dropdownValue)
    {
        return dropdownValue <= 0 ? BGMManager.SelectAllShuffle : dropdownValue - 1;
    }

    private void CreateBgmRow(Transform template, Slider bgmSlider)
    {
        if (template == null || bgmSlider == null)
            return;

        // BGM 볼륨 줄 바로 아래에 삽입
        Transform bgmRow = bgmSlider.transform.parent;
        Transform panel = bgmRow.parent;

        GameObject row = Instantiate(template.gameObject, panel, false);
        row.name = "Row_BGMSelect";
        row.SetActive(true);
        row.transform.SetSiblingIndex(bgmRow.GetSiblingIndex() + 1);

        // 높이를 BGM 볼륨 줄과 맞춤 (사운드 탭 영역 안에 들어가도록)
        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        LayoutElement bgmLayout = bgmRow.GetComponent<LayoutElement>();
        if (rowLayout != null && bgmLayout != null)
            rowLayout.preferredHeight = bgmLayout.preferredHeight;

        // 제목 / 설명
        Transform title = row.transform.Find("TitleText");
        if (title != null)
        {
            TMP_Text titleText = title.GetComponent<TMP_Text>();
            if (titleText != null) titleText.text = bgmSelectTitle;

            RectTransform titleRect = (RectTransform)title;
            titleRect.sizeDelta = new Vector2(400f, titleRect.sizeDelta.y);
        }

        Transform desc = row.transform.Find("DescriptionText");
        Transform bgmDesc = bgmRow.Find("DescriptionText");
        if (desc != null)
        {
            TMP_Text descText = desc.GetComponent<TMP_Text>();
            if (descText != null) descText.text = bgmSelectDescriptionText;

            // 설명 위치를 BGM 볼륨 줄과 같게
            if (bgmDesc != null)
                CopyRect((RectTransform)bgmDesc, (RectTransform)desc);
        }

        // 곡 선택 드롭다운 (슬라이더가 있던 오른쪽 영역)
        TMP_Dropdown songDropdown = row.GetComponentInChildren<TMP_Dropdown>(true);
        if (songDropdown == null)
        {
            Destroy(row);
            return;
        }

        songDropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
        songDropdown.gameObject.name = "BGMDropdown";

        // 슬라이더가 있던 오른쪽 영역에 배치
        RectTransform songRect = (RectTransform)songDropdown.transform;
        SetTopRight(songRect, Vector2.zero, new Vector2(700f, 50f));

        bgmSelectDropdowns.Add(songDropdown);
    }

    private static void SetTopRight(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void CopyRect(RectTransform from, RectTransform to)
    {
        to.anchorMin = from.anchorMin;
        to.anchorMax = from.anchorMax;
        to.pivot = from.pivot;
        to.anchoredPosition = from.anchoredPosition;
        to.sizeDelta = from.sizeDelta;
    }

    // 추가
    public void GoToMainMenu()
    {
        fadeManager.LoadScene("MainMenuScene");
    }
}