using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 설정씬 일반 탭에 "조작 감도" 줄을 추가한다. (마이크 입력 줄을 복제해서 같은 모양으로)
/// 오른쪽 조이스틱으로 방향을 바꿀 때의 회전 속도 배율을 정한다. (TurnSensitivity)
///
/// static 클래스라 어떤 오브젝트에도 붙일 필요 없음. (씬 파일 수정 없음)
/// </summary>
public static class TurnSensitivitySetup
{
    private const string RowName = "Row_TurnSensitivity";
    private const string Title = "조작 감도";
    private const string Description = "오른쪽 조이스틱으로 방향을 바꾸는 빠르기";

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
        if (ui == null || ui.genInputSlider == null)
            return;

        Transform templateRow = ui.genInputSlider.transform.parent;
        if (templateRow == null || templateRow.parent == null)
            return;

        Transform panel = templateRow.parent;
        if (panel.Find(RowName) != null)
            return;

        GameObject row = Object.Instantiate(templateRow.gameObject, panel, false);
        row.name = RowName;
        row.SetActive(true);
        row.transform.SetSiblingIndex(templateRow.GetSiblingIndex() + 1);

        // 마이크 전용 요소(입력 막대, 입김 보정 버튼) 제거
        foreach (MicLevelMeter meter in row.GetComponentsInChildren<MicLevelMeter>(true))
            Object.Destroy(meter.gameObject);
        foreach (MicCalibrationButton button in row.GetComponentsInChildren<MicCalibrationButton>(true))
            Object.Destroy(button.gameObject);

        // 제목 / 설명
        SetText(row.transform, "TitleText", Title);
        SetText(row.transform, "DescriptionText", Description);

        // 슬라이더 : 복제본에 남은 기존 연결을 모두 끊고 조작 감도에 연결
        Slider slider = row.GetComponentInChildren<Slider>(true);
        if (slider == null)
        {
            Object.Destroy(row);
            return;
        }

        slider.onValueChanged = new Slider.SliderEvent();
        slider.SetValueWithoutNotify(Mathf.Lerp(slider.minValue, slider.maxValue, TurnSensitivity.Value));
        slider.onValueChanged.AddListener(new UnityAction<float>(_ => TurnSensitivity.Set(slider.normalizedValue)));

        Debug.Log($"[TurnSensitivitySetup] 조작 감도 줄 추가 (현재 {TurnSensitivity.Value:F2}, 배율 x{TurnSensitivity.Multiplier:F2})");
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
