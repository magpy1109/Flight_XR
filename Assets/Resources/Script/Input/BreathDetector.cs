using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Android;

public class BreathDetector : MonoBehaviour
{
    public static BreathDetector Instance { get; private set; }

    // 최종 입김 세기
    public float BreathPower { get; private set; }

    // 디버깅용
    public float RawVolume { get; private set; }

    public bool MicrophoneReady { get; private set; }

    [Header("Microphone")]
    [SerializeField] private int sampleWindow = 512;
    [SerializeField] private int microphoneLength = 10;
    [SerializeField] private int sampleRate = 44100;

    [Header("Breath Settings")]
    [SerializeField] private float minVolume = 0.003f;
    [SerializeField] private float maxVolume = 0.04f;
    [SerializeField] private float smoothing = 8f;

    // ---------- 바람 소리 필터 ----------
    // 야외 바람도 마이크에는 "바람 소리"로 들어와서 입김처럼 인식될 수 있다. 다음 특징으로 구분한다.
    // 1) 꾸준함 : 입김은 일정한 세기로 이어지고, 바람은 세졌다 약해졌다 불규칙하다. (짧은 구간 세기 변동률)
    // 2) 소리 성분 : 입으로 부는 소리에는 "후~" 하는 높은 음역(쉬익) 성분이 섞여 있고, 바람은 낮은 음역 위주다.
    // 3) 배경 세기 : 계속 부는 바람 / 소음은 배경으로 학습해서 그만큼 빼고 계산한다.
    [Header("Wind Filter")]
    [Tooltip("바람 소리 필터 사용 (야외 플레이용)")]
    [SerializeField] private bool windFilter = true;

    [Tooltip("분석 구간 길이 (블록 수, 블록 1개 = 약 23ms)")]
    [SerializeField] private int steadinessBlocks = 8;

    [Tooltip("세기 변동률이 이 값 이하면 꾸준한 입김으로 봄")]
    [SerializeField] private float steadyVariation = 0.3f;

    [Tooltip("세기 변동률이 이 값 이상이면 불규칙한 바람으로 봄")]
    [SerializeField] private float gustyVariation = 0.7f;

    [Tooltip("높은 음역 비율이 이 값 이상이면 입김 소리 특징이 뚜렷함")]
    [SerializeField] private float breathHighRatio = 0.35f;

    [Tooltip("높은 음역 기준 주파수 (Hz)")]
    [SerializeField] private float highPassHz = 1000f;

    /// <summary>마지막 분석 결과 (디버깅 / 설정 화면용) : 0 = 바람 / 소음, 1 = 입김</summary>
    public float BreathLikelihood { get; private set; } = 1f;

    /// <summary>학습된 배경 소음 / 바람 세기</summary>
    public float AmbientLevel { get; private set; }

    private const int BlockSize = 1024;
    private float[] block;
    private float[] blockRms;
    private int blockIndex;
    private int blockCount;
    private int lastReadPosition = -1;
    private float hpPrevIn;
    private float hpPrevOut;
    private float lastHighRatio;
    private float breathConfidence = 1f;

    private AudioClip microphoneClip;
    private string microphoneDevice;

    private float targetPower;

    private float[] samples;

    private float debugTimer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        samples = new float[sampleWindow];
        block = new float[BlockSize];
        blockRms = new float[Mathf.Max(3, steadinessBlocks)];
    }

    private void Start()
    {
#if UNITY_EDITOR

        Debug.Log("BreathDetector : PC 테스트 모드");

#else

        StartCoroutine(StartMicrophone());

#endif
    }

    private IEnumerator StartMicrophone()
    {
#if UNITY_ANDROID

        // Android 마이크 권한
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Debug.Log("마이크 권한 요청");

            Permission.RequestUserPermission(
                Permission.Microphone);

            // 권한 팝업 처리 대기
            yield return new WaitForSeconds(2f);
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Debug.LogError("마이크 권한이 없습니다.");
            yield break;
        }

#endif

        yield return null;

        // 사용 가능한 마이크 확인
        if (Microphone.devices.Length > 0)
        {
            foreach (string device in Microphone.devices)
            {
                Debug.Log("발견된 마이크 : " + device);
            }

            // Quest에서는 우선 기본 마이크 사용
            microphoneDevice = null;

            Debug.Log("기본 마이크를 사용합니다.");
        }
        else
        {
            Debug.LogError("사용 가능한 마이크가 없습니다.");
            yield break;
        }

        // 마이크 시작
        microphoneClip = Microphone.Start(
            microphoneDevice,
            true,
            microphoneLength,
            sampleRate);

        if (microphoneClip == null)
        {
            Debug.LogError("Microphone.Start 실패");
            yield break;
        }

        // 실제 녹음 시작까지 대기
        float timeout = 3f;

        while (Microphone.GetPosition(microphoneDevice) <= 0)
        {
            yield return null;

            timeout -= Time.deltaTime;

            if (timeout <= 0f)
            {
                Debug.LogError(
                    "마이크 녹음 시작 대기 시간 초과");

                yield break;
            }
        }

        MicrophoneReady = true;

        Debug.Log("================================");
        Debug.Log("마이크 녹음 시작");
        Debug.Log("현재 마이크 : 기본 마이크");
        Debug.Log("================================");
    }

    private void Update()
    {
#if UNITY_EDITOR

        // PC 테스트
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.isPressed)
        {
            targetPower = 1f;
        }
        else if (Keyboard.current != null &&
                 Keyboard.current.wKey.isPressed)
        {
            targetPower = 0.5f;
        }
        else
        {
            targetPower = 0f;
        }

#else

        if (!MicrophoneReady ||
            microphoneClip == null)
        {
            targetPower = 0f;
        }
        else
        {
            RawVolume = GetMicrophoneVolume();

            float volume = RawVolume;

            if (windFilter)
            {
                AnalyzeNewBlocks();
                volume = ApplyWindFilter(RawVolume);
            }

            // 설정씬의 "입력" 감도를 반영
            targetPower = MicSensitivity.Evaluate(
                volume,
                minVolume,
                maxVolume);

            if (windFilter)
            {
                // 입김으로 확신할수록 그대로, 바람 / 소음으로 보이면 줄인다
                targetPower *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.7f, breathConfidence));
            }
        }

#endif

        // 부드럽게 변화
        BreathPower = Mathf.Lerp(
            BreathPower,
            targetPower,
            smoothing * Time.deltaTime);

        // 디버깅 로그
        debugTimer += Time.deltaTime;

        if (debugTimer >= 0.5f)
        {
            debugTimer = 0f;

            Debug.Log(
                $"MIC Ready={MicrophoneReady} | " +
                $"Raw={RawVolume:F5} | " +
                $"Ambient={AmbientLevel:F5} | " +
                $"Breath={BreathLikelihood:F2} (고음비율 {lastHighRatio:F2}) | " +
                $"Power={BreathPower:F3}");
        }
    }

    private float GetMicrophoneVolume()
    {
        int position =
            Microphone.GetPosition(microphoneDevice);

        if (position <= sampleWindow)
            return 0f;

        int startPosition =
            position - sampleWindow;

        microphoneClip.GetData(
            samples,
            startPosition);

        float sum = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }

        return Mathf.Sqrt(
            sum / samples.Length);
    }

    // ---------- 바람 소리 필터 ----------

    /// <summary>마이크 버퍼에 새로 들어온 소리를 약 23ms 블록 단위로 분석</summary>
    private void AnalyzeNewBlocks()
    {
        int position = Microphone.GetPosition(microphoneDevice);
        int total = microphoneClip.samples;

        if (lastReadPosition < 0)
        {
            lastReadPosition = position;
            return;
        }

        int available = position - lastReadPosition;
        if (available < 0)
            available += total;

        // 너무 밀렸으면(프레임 드랍 등) 최근 것만 분석
        if (available > BlockSize * 8)
        {
            lastReadPosition = (position - BlockSize * 8 + total) % total;
            available = BlockSize * 8;
        }

        while (available >= BlockSize)
        {
            microphoneClip.GetData(block, lastReadPosition);
            AnalyzeBlock(block);

            lastReadPosition = (lastReadPosition + BlockSize) % total;
            available -= BlockSize;
        }
    }

    private void AnalyzeBlock(float[] data)
    {
        // 1차 고역 통과 필터로 높은 음역 성분만 따로 계산
        float rc = 1f / (2f * Mathf.PI * highPassHz);
        float dt = 1f / Mathf.Max(8000, microphoneClip.frequency);
        float a = rc / (rc + dt);

        float sum = 0f;
        float highSum = 0f;

        for (int i = 0; i < data.Length; i++)
        {
            float x = data[i];
            float y = a * (hpPrevOut + x - hpPrevIn);
            hpPrevIn = x;
            hpPrevOut = y;

            sum += x * x;
            highSum += y * y;
        }

        float rms = Mathf.Sqrt(sum / data.Length);
        float highRms = Mathf.Sqrt(highSum / data.Length);

        lastHighRatio = rms > 1e-6f ? highRms / rms : 0f;

        blockRms[blockIndex] = rms;
        blockIndex = (blockIndex + 1) % blockRms.Length;
        blockCount = Mathf.Min(blockCount + 1, blockRms.Length);

        // 짧은 구간 세기 변동률 (표준편차 / 평균)
        float mean = 0f;
        for (int i = 0; i < blockCount; i++)
            mean += blockRms[i];
        mean /= Mathf.Max(1, blockCount);

        float variance = 0f;
        for (int i = 0; i < blockCount; i++)
        {
            float d = blockRms[i] - mean;
            variance += d * d;
        }
        variance /= Mathf.Max(1, blockCount);

        float variation = mean > 1e-5f ? Mathf.Sqrt(variance) / mean : 0f;

        // 꾸준함 (1 = 꾸준한 입김, 0 = 불규칙한 바람)
        float steadiness = 1f - Mathf.InverseLerp(steadyVariation, gustyVariation, variation);

        // 소리 성분 (입김 특징이 뚜렷하면 1, 낮은 음역만 있으면 0.6)
        float spectral = Mathf.Lerp(0.6f, 1f, Mathf.InverseLerp(breathHighRatio * 0.4f, breathHighRatio, lastHighRatio));

        BreathLikelihood = steadiness * spectral;

        // 입김 판정은 부드럽게 변하도록
        float blockTime = data.Length * dt;
        breathConfidence = Mathf.Lerp(breathConfidence, BreathLikelihood, 1f - Mathf.Exp(-blockTime / 0.12f));

        // 배경 세기 학습 : 조용해지면 빠르게 내려가고, 바람처럼 불규칙한 소리일 때만 천천히 올라간다
        if (rms < AmbientLevel)
            AmbientLevel = Mathf.Lerp(AmbientLevel, rms, 1f - Mathf.Exp(-blockTime / 0.5f));
        else if (BreathLikelihood < 0.5f)
            AmbientLevel = Mathf.Lerp(AmbientLevel, rms, 1f - Mathf.Exp(-blockTime / 3f));
        else
            AmbientLevel = Mathf.Lerp(AmbientLevel, rms, 1f - Mathf.Exp(-blockTime / 30f));
    }

    /// <summary>학습된 배경(바람 / 소음) 세기만큼 빼서 입김 부분만 남긴다</summary>
    private float ApplyWindFilter(float rms)
    {
        // 원래 입김 시작 기준(보정값 또는 minVolume)보다 배경이 클 때만 그 차이만큼 뺀다
        // (조용한 실내에서는 기존과 똑같이 동작)
        float baseline = MicSensitivity.HasCalibration
            ? Mathf.Max(MicSensitivity.NoiseFloor * 2f, 0.0005f)
            : minVolume;

        float extra = Mathf.Max(0f, AmbientLevel * 1.3f - baseline);
        return Mathf.Max(0f, rms - extra);
    }

    private void OnDestroy()
    {
#if !UNITY_EDITOR

        if (microphoneClip != null)
        {
            Microphone.End(microphoneDevice);
        }

#endif
    }
}