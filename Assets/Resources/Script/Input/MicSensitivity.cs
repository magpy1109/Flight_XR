using UnityEngine;

/// <summary>
/// 마이크 입력 감도 설정.
///
/// 1) 감도 (설정씬 "입력" 슬라이더, 0 ~ 1)
///    0 = 둔감(세게 불어야 함), 0.5 = 기본, 1 = 민감(약하게 불어도 됨)
///
/// 2) 입김 보정 (설정씬 "입김 보정" 버튼)
///    주변 소음 크기와 사용자가 실제로 불었을 때의 크기를 측정해 저장.
///    Quest 3S처럼 마이크가 입에서 먼 기기에서도 약한 입력을 최대 세기로 인식하게 해준다.
/// </summary>
public static class MicSensitivity
{
    private const string KEY_SENSITIVITY = "Setting_MicSensitivity";
    private const string KEY_NOISE = "Setting_MicNoiseFloor";
    private const string KEY_PEAK = "Setting_MicBlowPeak";

    public const float DefaultValue = 0.5f;

    // 감도 0 → 필요한 세기 x3.16 / 감도 1 → x0.32
    private const float RangeBase = 10f;

    // ---------- 감도 ----------

    /// <summary>저장된 감도 (0 ~ 1)</summary>
    public static float Value => Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_SENSITIVITY, DefaultValue));

    public static void Set(float value01)
    {
        PlayerPrefs.SetFloat(KEY_SENSITIVITY, Mathf.Clamp01(value01));
        PlayerPrefs.Save();
    }

    /// <summary>최대 세기로 판정할 볼륨에 곱할 값. 감도가 높을수록 작아진다.</summary>
    public static float VolumeMultiplier => Mathf.Pow(RangeBase, DefaultValue - Value);

    // ---------- 입김 보정 ----------

    public static bool HasCalibration => PlayerPrefs.HasKey(KEY_PEAK);

    /// <summary>보정 때 측정한 주변 소음 크기 (RMS)</summary>
    public static float NoiseFloor => PlayerPrefs.GetFloat(KEY_NOISE, 0f);

    /// <summary>보정 때 측정한 입김 크기 (RMS)</summary>
    public static float BlowPeak => PlayerPrefs.GetFloat(KEY_PEAK, 0f);

    public static void SaveCalibration(float noiseFloor, float blowPeak)
    {
        PlayerPrefs.SetFloat(KEY_NOISE, noiseFloor);
        PlayerPrefs.SetFloat(KEY_PEAK, blowPeak);
        PlayerPrefs.Save();
    }

    public static void ClearCalibration()
    {
        PlayerPrefs.DeleteKey(KEY_NOISE);
        PlayerPrefs.DeleteKey(KEY_PEAK);
        PlayerPrefs.Save();
    }

    // ---------- 계산 ----------

    /// <summary>
    /// 마이크 RMS 볼륨을 0 ~ 1 입김 세기로 변환.
    /// 보정값이 있으면 보정값을, 없으면 전달받은 기본 min/max를 사용하고 감도를 반영한다.
    /// </summary>
    public static float Evaluate(float rmsVolume, float defaultMin, float defaultMax)
    {
        float min = defaultMin;
        float max = defaultMax;

        if (HasCalibration)
        {
            // 주변 소음보다 확실히 큰 소리부터 입김으로 인식
            min = Mathf.Max(NoiseFloor * 2f, 0.0005f);

            // 보정 때 분 세기의 80%면 최대 세기
            max = Mathf.Max(min * 1.5f, BlowPeak * 0.8f);
        }

        // 감도는 "최대 세기 기준"에만 적용 (시작 기준은 소음 차단용으로 유지)
        float effectiveMax = Mathf.Max(min * 1.2f, max * VolumeMultiplier);

        return Mathf.InverseLerp(min, effectiveMax, rmsVolume);
    }
}
