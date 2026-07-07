using UnityEngine;

public class BreathDetector : MonoBehaviour
{
    public float currentVolume = 0f; 
    public float volumeMultiplier = 1000f; // 기본 증폭을 확 올렸어!

    private AudioClip micClip;
    private string deviceName;
    private int sampleWindow = 128; 

    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            deviceName = Microphone.devices[0]; 
            // 마이크 녹음 시작
            micClip = Microphone.Start(deviceName, true, 1, 44100);
            Debug.Log("🎙️ 마이크 연결 완료: " + deviceName);
        }
    }

    void Update()
    {
        float rawVolume = GetMicVolume();
        
        // 우리가 뻥튀기한 목표 볼륨값
        float targetVolume = rawVolume * volumeMultiplier;

        // 🌟 마법의 코드: 올라갈 때는 즉각적으로! 떨어질 때는 서서히!
        if (targetVolume > currentVolume)
        {
            // 더 큰 소리가 들어오면 바로 적용 (훅 불 때)
            currentVolume = targetVolume; 
        }
        else
        {
            // 소리가 줄어들면 서서히 0으로 깎임 (Time.deltaTime 사용)
            // 끝의 5f 숫자를 조절하면 떨어지는 속도를 바꿀 수 있어. (작을수록 천천히 떨어짐)
            currentVolume = Mathf.Lerp(currentVolume, 0f, Time.deltaTime * 5f);
        }

        // 볼륨이 어느 정도 올라왔을 때만 확실하게 콘솔에 띄우기
        if (currentVolume > 0.1f)
        {
            Debug.Log("🌬️ 현재 입김 파워: " + currentVolume);
        }
    }

    float GetMicVolume()
    {
        if (deviceName == null) return 0f;

        // 현재 마이크가 녹음하고 있는 '위치(Position)'를 가져옴
        int micPosition = Microphone.GetPosition(deviceName);
        
        // 🚨 중요 탐지기: 마이크 위치가 0에 멈춰있다면 녹음이 안 되고 있는 거임!
        Debug.Log("마이크 녹음 위치: " + micPosition);

        // 녹음 위치가 우리가 필요한 샘플 개수보다 작으면 안전하게 0 반환
        if (micPosition < sampleWindow) return 0f;

        float[] waveData = new float[sampleWindow];
        // 녹음된 파형 데이터 가져오기
        micClip.GetData(waveData, micPosition - sampleWindow);

        // 평균값이 아니라, 들어온 소리 중 '가장 큰 소리(Peak)'만 뽑아내기!
        float maxVolume = 0f;
        for (int i = 0; i < sampleWindow; i++)
        {
            float wavePeak = waveData[i] * waveData[i];
            if (maxVolume < wavePeak)
            {
                maxVolume = wavePeak;
            }
        }
        
        return Mathf.Sqrt(maxVolume);
    }
}