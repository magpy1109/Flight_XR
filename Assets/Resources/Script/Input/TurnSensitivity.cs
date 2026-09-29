using UnityEngine;

/// <summary>
/// 방향 전환(오른쪽 조이스틱) 감도 설정.
/// 0 = 느리게(0.4배), 0.5 = 기본(1배), 1 = 빠르게(1.6배)
/// 설정씬 일반 탭 / 일시정지 설정의 "조작 감도" 슬라이더와 연결된다.
/// </summary>
public static class TurnSensitivity
{
    private const string Key = "Setting_TurnSensitivity";

    public const float DefaultValue = 0.5f;
    private const float MinMultiplier = 0.4f;
    private const float MaxMultiplier = 1.6f;

    /// <summary>저장된 감도 (0 ~ 1)</summary>
    public static float Value => Mathf.Clamp01(PlayerPrefs.GetFloat(Key, DefaultValue));

    public static void Set(float value01)
    {
        PlayerPrefs.SetFloat(Key, Mathf.Clamp01(value01));
        PlayerPrefs.Save();
    }

    /// <summary>조이스틱 값에 곱하는 배율</summary>
    public static float Multiplier
    {
        get
        {
            float v = Value;
            // 0 → 0.4, 0.5 → 1.0, 1 → 1.6
            return v <= 0.5f
                ? Mathf.Lerp(MinMultiplier, 1f, v / 0.5f)
                : Mathf.Lerp(1f, MaxMultiplier, (v - 0.5f) / 0.5f);
        }
    }
}
