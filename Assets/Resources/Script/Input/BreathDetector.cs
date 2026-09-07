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

            targetPower = Mathf.InverseLerp(
                minVolume,
                maxVolume,
                RawVolume);
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