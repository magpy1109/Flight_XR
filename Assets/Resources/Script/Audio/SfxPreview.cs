using UnityEngine;

/// <summary>
/// 효과음 볼륨을 바꿀 때 바뀐 크기로 짧은 확인음("띵")을 들려준다.
///
/// - SFX 믹서 그룹으로 재생하므로 효과음 / 마스터 볼륨, 음소거가 그대로 적용된다.
/// - 슬라이더를 끄는 동안 너무 자주 울리지 않도록 간격을 둔다.
/// - 일시정지 중(AudioListener.pause)에도 들리도록 한다.
/// - 확인음은 실행 시 코드로 만들어서 별도 사운드 파일이 필요 없다.
/// </summary>
public static class SfxPreview
{
    private const float MinInterval = 0.12f;   // 확인음 최소 간격(초)
    private const float Loudness = 0.6f;

    private static AudioSource source;
    private static AudioClip clip;
    private static float lastPlayTime = -10f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        source = null;
        clip = null;
        lastPlayTime = -10f;
    }

    public static void Play()
    {
        if (Time.unscaledTime - lastPlayTime < MinInterval)
            return;

        AudioSource src = GetSource();
        if (src == null)
            return;

        lastPlayTime = Time.unscaledTime;

        if (clip == null)
            clip = CreateChime();

        src.PlayOneShot(clip, Loudness);
    }

    private static AudioSource GetSource()
    {
        if (source != null)
            return source;

        // 효과음 매니저(씬이 바뀌어도 유지됨)에 붙여서 같은 믹서 그룹 사용
        GameObject owner;
        UnityEngine.Audio.AudioMixerGroup group = null;

        if (SFXManager.Instance != null)
        {
            owner = SFXManager.Instance.gameObject;
            group = SFXManager.Instance.sfxMixerGroup;
        }
        else
        {
            owner = new GameObject("[SfxPreview]");
            Object.DontDestroyOnLoad(owner);
        }

        source = owner.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;

        if (group != null)
            source.outputAudioMixerGroup = group;

        return source;
    }

    /// <summary>부드러운 두 음 "띵-" (약 0.25초)</summary>
    private static AudioClip CreateChime()
    {
        const int sampleRate = 44100;
        const float length = 0.25f;
        int samples = Mathf.CeilToInt(sampleRate * length);

        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;

            // 첫 음 (A5) + 조금 뒤 둘째 음 (E6)
            float note1 = Tone(t, 880f, 0f);
            float note2 = Tone(t, 1318.5f, 0.06f);

            // 시작 부분 클릭음 방지
            float attack = Mathf.Clamp01(t / 0.004f);

            data[i] = (note1 * 0.55f + note2 * 0.45f) * attack;
        }

        AudioClip chime = AudioClip.Create("SfxPreviewChime", samples, 1, sampleRate, false);
        chime.SetData(data, 0);
        return chime;
    }

    private static float Tone(float t, float frequency, float delay)
    {
        float local = t - delay;
        if (local < 0f)
            return 0f;

        float attack = Mathf.Clamp01(local / 0.004f);
        float decay = Mathf.Exp(-local * 18f);

        // 기본음 + 약한 배음으로 부드러운 종소리 느낌
        float wave = Mathf.Sin(2f * Mathf.PI * frequency * local)
                   + 0.25f * Mathf.Sin(2f * Mathf.PI * frequency * 2f * local);

        return wave * attack * decay * 0.8f;
    }
}
