using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 마스터 / BGM / SFX 볼륨과 음소거 상태를 AudioMixer에 실제로 적용하고
/// PlayerPrefs에 저장/로드하는 매니저.
///
/// [사전 준비 - Unity 에디터에서 할 일]
/// 1. Project 창에서 Create > Audio Mixer 로 믹서 생성 (예: MainMixer)
/// 2. 믹서 안에 그룹 3개 생성: Master(기본) 하위에 BGM, SFX 그룹 추가
/// 3. 각 그룹의 Volume 파라미터를 우클릭 > "Expose 'Volume' to script" 로 노출
/// 4. 노출된 파라미터 이름을 각각 MasterVolume / BGMVolume / SFXVolume 로 rename
///    (Edit > Exposed Parameters 패널에서 이름 변경)
/// 5. 씬에 빈 오브젝트 생성 후 이 스크립트를 붙이고 audioMixer 필드에 방금 만든 믹서 연결
/// 6. BGMManager, SFXManager 의 AudioSource 들이 각각 BGM/SFX 그룹으로 라우팅되도록 연결
/// </summary>
public class AudioSettingsManager : MonoBehaviour
{
    public static AudioSettingsManager Instance { get; private set; }

    [Header("[ Audio Mixer ]")]
    public AudioMixer audioMixer;

    [Header("[ Exposed Parameter Names ]")]
    [SerializeField] private string masterParam = "MasterVolume";
    [SerializeField] private string bgmParam = "BGMVolume";
    [SerializeField] private string sfxParam = "SFXVolume";

    private const string KEY_MASTER = "Setting_MasterVolume";
    private const string KEY_BGM = "Setting_BGMVolume";
    private const string KEY_SFX = "Setting_SFXVolume";
    private const string KEY_MASTER_MUTE = "Setting_MasterMute";
    private const string KEY_BGM_MUTE = "Setting_BGMMute";
    private const string KEY_SFX_MUTE = "Setting_SFXMute";

    // 음소거를 풀 때 되돌아갈 이전 볼륨 값
    private float lastMasterVolume = 1f;
    private float lastBGMVolume = 1f;
    private float lastSFXVolume = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAndApplySettings();
    }

    private void LoadAndApplySettings()
    {
        lastMasterVolume = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
        lastBGMVolume = PlayerPrefs.GetFloat(KEY_BGM, 1f);
        lastSFXVolume = PlayerPrefs.GetFloat(KEY_SFX, 1f);

        bool masterMuted = PlayerPrefs.GetInt(KEY_MASTER_MUTE, 0) == 1;
        bool bgmMuted = PlayerPrefs.GetInt(KEY_BGM_MUTE, 0) == 1;
        bool sfxMuted = PlayerPrefs.GetInt(KEY_SFX_MUTE, 0) == 1;

        ApplyVolume(masterParam, masterMuted ? 0f : lastMasterVolume);
        ApplyVolume(bgmParam, bgmMuted ? 0f : lastBGMVolume);
        ApplyVolume(sfxParam, sfxMuted ? 0f : lastSFXVolume);
    }

    // ---------- 볼륨 ----------
    public void SetMasterVolume(float value01)
    {
        lastMasterVolume = value01;
        if (!GetSavedMasterMute()) ApplyVolume(masterParam, value01);
        PlayerPrefs.SetFloat(KEY_MASTER, value01);
    }

    public void SetBGMVolume(float value01)
    {
        lastBGMVolume = value01;
        if (!GetSavedBGMMute()) ApplyVolume(bgmParam, value01);
        PlayerPrefs.SetFloat(KEY_BGM, value01);
    }

    public void SetSFXVolume(float value01)
    {
        lastSFXVolume = value01;
        if (!GetSavedSFXMute()) ApplyVolume(sfxParam, value01);
        PlayerPrefs.SetFloat(KEY_SFX, value01);
    }

    private void ApplyVolume(string param, float value01)
    {
        if (audioMixer == null) return;
        // 0에 가까울 때 -Infinity dB 방지용 최소값 클램프
        float clamped = Mathf.Clamp(value01, 0.0001f, 1f);
        float dB = Mathf.Log10(clamped) * 20f;
        audioMixer.SetFloat(param, dB);
    }

    // ---------- 음소거 ----------
    public void SetMasterMute(bool mute)
    {
        ApplyVolume(masterParam, mute ? 0f : lastMasterVolume);
        PlayerPrefs.SetInt(KEY_MASTER_MUTE, mute ? 1 : 0);
    }

    public void SetBGMMute(bool mute)
    {
        ApplyVolume(bgmParam, mute ? 0f : lastBGMVolume);
        PlayerPrefs.SetInt(KEY_BGM_MUTE, mute ? 1 : 0);
    }

    public void SetSFXMute(bool mute)
    {
        ApplyVolume(sfxParam, mute ? 0f : lastSFXVolume);
        PlayerPrefs.SetInt(KEY_SFX_MUTE, mute ? 1 : 0);
    }

    // ---------- 저장값 조회 (UI 초기화용) ----------
    public float GetSavedMasterVolume() => PlayerPrefs.GetFloat(KEY_MASTER, 1f);
    public float GetSavedBGMVolume() => PlayerPrefs.GetFloat(KEY_BGM, 1f);
    public float GetSavedSFXVolume() => PlayerPrefs.GetFloat(KEY_SFX, 1f);
    public bool GetSavedMasterMute() => PlayerPrefs.GetInt(KEY_MASTER_MUTE, 0) == 1;
    public bool GetSavedBGMMute() => PlayerPrefs.GetInt(KEY_BGM_MUTE, 0) == 1;
    public bool GetSavedSFXMute() => PlayerPrefs.GetInt(KEY_SFX_MUTE, 0) == 1;
}