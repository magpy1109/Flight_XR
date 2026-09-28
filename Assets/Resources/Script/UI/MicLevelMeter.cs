using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// 설정씬 "입력" 줄 아래에 표시되는 실시간 마이크 입력 막대.
/// 게임에서 쓰는 것과 같은 계산(MicSensitivity.Evaluate)으로 입김 세기를 보여주므로
/// 감도 슬라이더를 움직이면서 바로 확인할 수 있다.
///
/// 마이크는 이 막대가 화면에 보이는 동안만 켜진다. (여러 개여도 마이크는 하나만 사용)
/// </summary>
public class MicLevelMeter : MonoBehaviour
{
    // 게임 씬(BreathDetector)과 같은 기준값
    private const float MinVolume = 0.003f;
    private const float MaxVolume = 0.03f;

    private const int SampleRate = 44100;
    private const int SampleWindow = 256;

    // ---------- 공용 마이크 (막대 여러 개가 공유) ----------
    private static int activeMeters;
    private static AudioClip micClip;
    private static bool micRunning;
    private static readonly float[] samples = new float[SampleWindow];
    private static int lastSampledFrame = -1;
    private static float lastRms;

    // Enter Play Mode 설정에서 도메인 리로드를 끈 경우를 대비해 static 값 초기화
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        activeMeters = 0;
        micClip = null;
        micRunning = false;
        lastSampledFrame = -1;
        lastRms = 0f;
    }

    /// <summary>마이크가 켜져 있는지 (입력 막대가 화면에 보일 때만 켜짐)</summary>
    public static bool IsMicRunning => micRunning;

    /// <summary>현재 마이크 볼륨 (RMS)</summary>
    public static float CurrentRms => micRunning ? GetRms() : 0f;

    private RectTransform fillRect;
    private float shownLevel;

    /// <summary>슬라이더 아래에 막대를 만든다.</summary>
    public static MicLevelMeter Create(Slider slider)
    {
        RectTransform sliderRect = (RectTransform)slider.transform;
        Transform row = sliderRect.parent;

        // 배경 막대
        GameObject bg = new GameObject("MicLevelMeter", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(row, false);

        RectTransform bgRect = (RectTransform)bg.transform;
        bgRect.anchorMin = sliderRect.anchorMin;
        bgRect.anchorMax = sliderRect.anchorMax;
        bgRect.pivot = sliderRect.pivot;
        bgRect.anchoredPosition = sliderRect.anchoredPosition + new Vector2(0f, -sliderRect.sizeDelta.y - 5f);
        bgRect.sizeDelta = new Vector2(sliderRect.sizeDelta.x, 10f);

        Image bgImage = bg.GetComponent<Image>();
        bgImage.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        bgImage.raycastTarget = false;

        // 채워지는 막대
        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(bg.transform, false);

        RectTransform fRect = (RectTransform)fill.transform;
        fRect.anchorMin = Vector2.zero;
        fRect.anchorMax = new Vector2(0f, 1f);
        fRect.pivot = new Vector2(0f, 0.5f);
        fRect.offsetMin = Vector2.zero;
        fRect.offsetMax = Vector2.zero;

        Image fillImage = fill.GetComponent<Image>();
        ColorUtility.TryParseHtmlString("#0C8CE9", out Color blue);
        fillImage.color = blue;
        fillImage.raycastTarget = false;

        MicLevelMeter meter = bg.AddComponent<MicLevelMeter>();
        meter.fillRect = fRect;
        return meter;
    }

    private void OnEnable()
    {
        activeMeters++;

        if (activeMeters == 1)
            StartMic();
    }

    private void OnDisable()
    {
        activeMeters = Mathf.Max(0, activeMeters - 1);

        if (activeMeters == 0)
            StopMic();

        shownLevel = 0f;
        SetFill(0f);
    }

    private void Update()
    {
        if (!micRunning)
        {
            // 권한 팝업을 허용한 뒤에 다시 시도
            TryStartMicIfPermitted();
            return;
        }

        float target = MicSensitivity.Evaluate(GetRms(), MinVolume, MaxVolume);
        shownLevel = Mathf.Lerp(shownLevel, target, Time.unscaledDeltaTime * 10f);
        SetFill(shownLevel);
    }

    private void SetFill(float level01)
    {
        if (fillRect != null)
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(level01), 1f);
    }

    // ---------- 마이크 ----------

    private static void StartMic()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            return;
        }
#endif
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[MicLevelMeter] 사용 가능한 마이크가 없습니다.");
            return;
        }

        micClip = Microphone.Start(null, true, 1, SampleRate);
        micRunning = micClip != null;
    }

    private static void TryStartMicIfPermitted()
    {
        if (micRunning || activeMeters == 0)
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            return;

        StartMic();
#endif
    }

    private static void StopMic()
    {
        if (micRunning)
            Microphone.End(null);

        micRunning = false;
        micClip = null;
        lastRms = 0f;
    }

    private static float GetRms()
    {
        // 같은 프레임에 막대가 여러 개여도 한 번만 계산
        if (lastSampledFrame == Time.frameCount)
            return lastRms;

        lastSampledFrame = Time.frameCount;

        if (micClip == null)
            return lastRms = 0f;

        int position = Microphone.GetPosition(null);

        if (position < SampleWindow)
            return lastRms;

        micClip.GetData(samples, position - SampleWindow);

        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
            sum += samples[i] * samples[i];

        lastRms = Mathf.Sqrt(sum / samples.Length);
        return lastRms;
    }
}
