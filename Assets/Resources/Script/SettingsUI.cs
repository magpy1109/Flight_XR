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
    public TMP_Dropdown genResDropdown;
    public TMP_Dropdown genSizeDropdown;
    public TMP_Dropdown genFrameDropdown;

    [Header("[ Individual Tab UI Elements ]")]
    public Slider soundInputSlider;
    public Slider soundMasterSlider;
    public Slider soundBGMSlider;
    public Slider soundSFXSlider;
    public TMP_Dropdown graphResDropdown;
    public TMP_Dropdown graphSizeDropdown;
    public TMP_Dropdown graphFrameDropdown;

    [Header("[ Mute Row Sync ]")] // ★ [새로 추가] 각 줄의 음소거 컴포넌트 연동 칸
    public SoundRowMuter genMasterMuter;
    public SoundRowMuter soundMasterMuter;
    public SoundRowMuter genBGMMuter;
    public SoundRowMuter soundBGMMuter;
    public SoundRowMuter genSFXMuter;
    public SoundRowMuter soundSFXMuter;

    private bool isSyncing = false;
    private Color activeTextColor;
    private Color inactiveTextColor;

    void Start()
    {
        ColorUtility.TryParseHtmlString("#0C8CE9", out activeTextColor);
        ColorUtility.TryParseHtmlString("#1A1A1A", out inactiveTextColor);

        tabGeneralBtn.onClick.AddListener(() => SwitchTab(0));
        tabSoundBtn.onClick.AddListener(() => SwitchTab(1));
        tabGraphicBtn.onClick.AddListener(() => SwitchTab(2));

        // 슬라이더 값 동기화
        SetupSliderSync(genInputSlider, soundInputSlider);
        SetupSliderSync(genMasterSlider, soundMasterSlider);
        SetupSliderSync(genBGMSlider, soundBGMSlider);
        SetupSliderSync(genSFXSlider, soundSFXSlider);

        // 드롭다운 값 동기화
        SetupDropdownSync(genResDropdown, graphResDropdown);
        SetupDropdownSync(genSizeDropdown, graphSizeDropdown);
        SetupDropdownSync(genFrameDropdown, graphFrameDropdown);

        // ★ [새로 추가] 줄 음소거 거울 동기화 연결
        SetupMuteSync(genMasterMuter, soundMasterMuter);
        SetupMuteSync(genBGMMuter, soundBGMMuter);
        SetupMuteSync(genSFXMuter, soundSFXMuter);

        SwitchTab(0);
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

    // ★ [새로 추가] 음소거 상태를 실시간 거울 동기화 시켜주는 기하학적 함수
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

    // 추가
    public void GoToMainMenu()
    {
        fadeManager.LoadScene("MainMenuScene");
    }
}