using UnityEngine;

/// <summary>
/// 그래픽 탭의 "프레임" 드롭다운 값을 실제 헤드셋 디스플레이 주사율에 적용.
/// 프로젝트에 Meta XR SDK(OVRManager)가 포함되어 있다는 전제로 작성했습니다.
///
/// [주의] frameRateOptions 배열의 순서는 UI 드롭다운(Dropdown)에 등록된
/// 옵션 문자열 순서와 반드시 1:1로 일치해야 합니다.
/// 예: 드롭다운 옵션이 [72fps, 80fps, 90fps, 120fps] 순서면
///     frameRateOptions = {72, 80, 90, 120} 로 동일한 순서 유지.
/// </summary>
public class FrameRateSettingsManager : MonoBehaviour
{
    public static FrameRateSettingsManager Instance { get; private set; }

    private const string KEY_FRAME = "Setting_FrameRateIndex";

    [Header("[ Frame Rate Options (Hz) - 드롭다운 순서와 반드시 일치 ]")]
    public int[] frameRateOptions = { 72, 80, 90, 120 };

    [Header("[ Default ]")]
    [Tooltip("저장된 값이 없을 때 기본으로 맞출 Hz 값")]
    public int defaultHz = 90;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApplyFrameRate(GetSavedIndex());
    }

    /// <summary>드롭다운 onValueChanged 에 바로 연결해서 쓰는 함수.</summary>
    public void ApplyFrameRate(int dropdownIndex)
    {
        if (dropdownIndex < 0 || dropdownIndex >= frameRateOptions.Length)
        {
            Debug.LogWarning("[FrameRateSettingsManager] 드롭다운 인덱스가 frameRateOptions 범위를 벗어났습니다.");
            return;
        }

        int hz = frameRateOptions[dropdownIndex];

        // Meta Quest 헤드셋 디스플레이 주사율 변경
        if (OVRManager.instance != null)
        {
            bool applied = OVRManager.display.displayFrequency != hz
                ? SetOvrDisplayFrequency(hz)
                : true;

            if (!applied)
            {
                Debug.LogWarning($"[FrameRateSettingsManager] {hz}Hz는 이 기기에서 지원하지 않는 주사율일 수 있습니다.");
            }
        }

        // 앱 자체 목표 프레임레이트도 함께 맞춰줌
        Application.targetFrameRate = hz;

        PlayerPrefs.SetInt(KEY_FRAME, dropdownIndex);
    }

    private bool SetOvrDisplayFrequency(float hz)
    {
        OVRManager.display.displayFrequency = hz;
        return true;
    }

    public int GetSavedIndex()
    {
        int saved = PlayerPrefs.GetInt(KEY_FRAME, -1);
        if (saved >= 0 && saved < frameRateOptions.Length) return saved;

        for (int i = 0; i < frameRateOptions.Length; i++)
            if (frameRateOptions[i] == defaultHz) return i;

        return frameRateOptions.Length - 1;
    }
}