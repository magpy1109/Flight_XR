using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 그래픽 탭의 "프레임" 드롭다운 값을 실제 헤드셋 디스플레이 주사율에 적용.
/// 프로젝트에 Meta XR SDK(OVRManager)가 포함되어 있다는 전제로 작성했습니다.
///
/// 드롭다운 옵션 문구는 SettingsUI가 GetOptionLabels()로 채우므로
/// frameRateOptions 순서와 드롭다운 순서가 항상 같다.
/// </summary>
public class FrameRateSettingsManager : MonoBehaviour
{
    public static FrameRateSettingsManager Instance { get; private set; }

    private const string KEY_FRAME = "Setting_FrameRateIndex";

    [Header("[ Frame Rate Options (Hz) ]")]
    [Tooltip("Quest 계열이 지원하는 주사율. 드롭다운 옵션은 이 배열로 자동 생성됩니다.")]
    public int[] frameRateOptions = { 72, 80, 90, 120 };

    [Header("[ Default ]")]
    [Tooltip("저장된 값이 없을 때 기본으로 맞출 Hz 값")]
    public int defaultHz = 90;

    [Header("[ Startup ]")]
    [Tooltip("시작 시 OVRManager가 준비될 때까지 기다리는 최대 시간(초)")]
    public float waitForHeadsetTimeout = 10f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 이 매니저는 씬 로드 전에 생성되므로 OVRManager가 아직 없다.
        // 헤드셋이 준비된 뒤에 저장된 주사율을 적용한다.
        StartCoroutine(ApplySavedWhenReady());
    }

    private IEnumerator ApplySavedWhenReady()
    {
        float elapsed = 0f;

        while (!IsHeadsetReady() && elapsed < waitForHeadsetTimeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ApplyFrameRate(GetSavedIndex(), false);
    }

    private static bool IsHeadsetReady()
    {
        return OVRManager.instance != null &&
               OVRManager.display != null &&
               OVRManager.isHmdPresent;
    }

    /// <summary>드롭다운 onValueChanged 에 바로 연결해서 쓰는 함수.</summary>
    public void ApplyFrameRate(int dropdownIndex)
    {
        ApplyFrameRate(dropdownIndex, true);
    }

    private void ApplyFrameRate(int dropdownIndex, bool save)
    {
        if (dropdownIndex < 0 || dropdownIndex >= frameRateOptions.Length)
        {
            Debug.LogWarning("[FrameRateSettingsManager] 드롭다운 인덱스가 frameRateOptions 범위를 벗어났습니다.");
            return;
        }

        int hz = frameRateOptions[dropdownIndex];

        if (IsHeadsetReady())
        {
            float target = GetSupportedFrequency(hz);

            if (!Mathf.Approximately(target, hz))
            {
                Debug.LogWarning(
                    $"[FrameRateSettingsManager] {hz}Hz는 이 기기에서 지원하지 않아 {target}Hz로 적용합니다. " +
                    "(Quest 2의 120Hz는 헤드셋 설정 > 실험실 기능에서 켜야 합니다)");
            }

            if (!Mathf.Approximately(OVRManager.display.displayFrequency, target))
            {
                OVRManager.display.displayFrequency = target;
            }

            StartCoroutine(LogAppliedFrequency(target));
        }
        else
        {
            Debug.Log($"[FrameRateSettingsManager] 헤드셋이 연결되지 않아 {hz}Hz는 저장만 합니다.");
        }

        // XR 실행 중에는 헤드셋 주사율이 프레임을 결정하므로 이 값은 무시된다.
        // (헤드셋 없이 에디터에서 실행할 때만 의미 있음)
        Application.targetFrameRate = hz;

        if (save)
        {
            PlayerPrefs.SetInt(KEY_FRAME, dropdownIndex);
            PlayerPrefs.Save();
        }
    }

    /// <summary>기기가 지원하는 주사율 중 요청값과 같거나 가장 가까운 값</summary>
    private static float GetSupportedFrequency(int hz)
    {
        float[] available = OVRManager.display.displayFrequenciesAvailable;

        if (available == null || available.Length == 0)
            return hz;

        float best = available[0];

        foreach (float f in available)
        {
            if (Mathf.Abs(f - hz) < Mathf.Abs(best - hz))
                best = f;
        }

        return best;
    }

    private IEnumerator LogAppliedFrequency(float requested)
    {
        // 주사율 변경은 한두 프레임 뒤에 반영된다.
        yield return null;
        yield return null;

        if (IsHeadsetReady())
        {
            float current = OVRManager.display.displayFrequency;
            Debug.Log($"[FrameRateSettingsManager] 요청 {requested}Hz / 현재 헤드셋 주사율 {current}Hz");
        }
    }

    /// <summary>드롭다운에 표시할 옵션 문구 (frameRateOptions와 같은 순서)</summary>
    public List<string> GetOptionLabels()
    {
        var labels = new List<string>();
        foreach (int hz in frameRateOptions)
            labels.Add($"{hz} FPS");
        return labels;
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
