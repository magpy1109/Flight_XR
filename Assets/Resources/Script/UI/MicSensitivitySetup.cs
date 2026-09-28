using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 설정씬이 로드되면 "입력" 슬라이더(일반 탭 / 사운드 탭)를 마이크 감도 설정에 연결하고,
/// 슬라이더 아래에 실시간 마이크 입력 막대를 추가한다.
///
/// static 클래스라 어떤 오브젝트에도 붙일 필요 없음.
/// (씬 파일이나 SettingsUI.cs를 수정하지 않고 동작하도록 분리)
/// </summary>
public static class MicSensitivitySetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 설정씬에서 바로 실행한 경우
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

        Setup(ui.genInputSlider);
        Setup(ui.soundInputSlider);

        Debug.Log($"[MicSensitivitySetup] 마이크 감도 슬라이더 연결 (현재 감도 {MicSensitivity.Value:F2})");
    }

    private static void Setup(Slider slider)
    {
        if (slider == null)
            return;

        // 이미 연결된 경우 중복 방지
        if (slider.transform.parent != null &&
            slider.transform.parent.GetComponentInChildren<MicLevelMeter>(true) != null)
            return;

        // 저장된 감도 표시 (슬라이더 범위와 무관하게 0~1로 저장)
        slider.SetValueWithoutNotify(Mathf.Lerp(slider.minValue, slider.maxValue, MicSensitivity.Value));

        slider.onValueChanged.AddListener(_ =>
        {
            MicSensitivity.Set(slider.normalizedValue);
        });

        MicLevelMeter.Create(slider);

        // 입김 보정 버튼 (Quest 3S처럼 마이크가 입에서 먼 기기용)
        MicCalibrationButton.Create(slider);
    }
}
