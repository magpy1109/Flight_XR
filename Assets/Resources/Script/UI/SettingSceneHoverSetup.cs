using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 설정씬(SettingsUI가 있는 씬)이 로드되면
/// 왼쪽 탭 버튼과 오른쪽 볼륨 슬라이더에 호버 확대 효과(UIHoverScale)를 자동으로 붙인다.
///
/// static 클래스라 어떤 오브젝트에도 붙일 필요 없음.
/// (씬 파일이나 SettingsUI.cs를 수정하지 않고 동작하도록 분리)
/// </summary>
public static class SettingSceneHoverSetup
{
    private const float TabHoverScale = 1.1f;
    private const float SliderHoverScale = 1.05f;

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

        // 왼쪽 탭 버튼
        Add(ui.tabGeneralBtn, TabHoverScale);
        Add(ui.tabSoundBtn, TabHoverScale);
        Add(ui.tabGraphicBtn, TabHoverScale);

        // 오른쪽 볼륨 슬라이더 (일반 탭 + 사운드 탭)
        Add(ui.genInputSlider, SliderHoverScale);
        Add(ui.genMasterSlider, SliderHoverScale);
        Add(ui.genBGMSlider, SliderHoverScale);
        Add(ui.genSFXSlider, SliderHoverScale);
        Add(ui.soundInputSlider, SliderHoverScale);
        Add(ui.soundMasterSlider, SliderHoverScale);
        Add(ui.soundBGMSlider, SliderHoverScale);
        Add(ui.soundSFXSlider, SliderHoverScale);

        Debug.Log("[SettingSceneHoverSetup] 탭 버튼 / 슬라이더 호버 확대 효과 적용");
    }

    private static void Add(Selectable target, float scale)
    {
        if (target == null)
            return;

        UIHoverScale hover = target.GetComponent<UIHoverScale>();

        if (hover == null)
            hover = target.gameObject.AddComponent<UIHoverScale>();

        hover.hoverScale = scale;
    }
}
