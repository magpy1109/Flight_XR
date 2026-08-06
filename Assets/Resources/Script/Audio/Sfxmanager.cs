using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 효과음 재생 매니저.
/// 효과음 종류(카테고리)가 아직 정해지지 않았기 때문에, 특정 enum이나 ID에
/// 묶지 않고 AudioClip을 직접 넘겨서 재생하는 범용 구조로 만들었습니다.
///
/// 나중에 종류가 정해지면:
///  - 소규모면: 그냥 각 스크립트에서 SFXManager.Instance.Play(clip) 로 호출
///  - 종류가 많아지면: SFXLibrary(ScriptableObject 등)로 "문자열 ID -> AudioClip" 매핑을
///    따로 만들고 Play(string id) 오버로드만 추가하면 됨 (이 파일 구조 변경 불필요)
/// </summary>
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("[ Audio Mixer Group ]")]
    [Tooltip("AudioSettingsManager 의 SFXVolume 파라미터가 걸려있는 믹서 그룹")]
    public AudioMixerGroup sfxMixerGroup;

    [Header("[ Pool Settings ]")]
    [Tooltip("동시에 재생 가능한 효과음 개수. 효과음 종류/빈도가 늘어나면 늘려주세요.")]
    public int poolSize = 10;

    private readonly List<AudioSource> pool = new List<AudioSource>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            if (sfxMixerGroup != null) src.outputAudioMixerGroup = sfxMixerGroup;
            pool.Add(src);
        }
    }

    /// <summary>효과음 1회 재생. volumeScale/pitch로 개별 클립마다 미세 조정 가능.</summary>
    public void Play(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        AudioSource src = GetAvailableSource();
        src.clip = clip;
        src.volume = volumeScale;
        src.pitch = pitch;
        src.Play();
    }

    /// <summary>랜덤 피치를 살짝 줘서 반복 재생 시 단조로움을 줄이고 싶을 때 사용.</summary>
    public void PlayWithRandomPitch(AudioClip clip, float volumeScale = 1f, float pitchMin = 0.95f, float pitchMax = 1.05f)
    {
        Play(clip, volumeScale, Random.Range(pitchMin, pitchMax));
    }

    private AudioSource GetAvailableSource()
    {
        foreach (var src in pool)
        {
            if (!src.isPlaying) return src;
        }

        Debug.LogWarning("[SFXManager] SFX 풀이 가득 찼습니다. poolSize를 늘리는 걸 고려하세요.");
        return pool[0];
    }
}