using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 충돌 효과음 (비행기 종류별).
///
/// Resources/Sound/Crash/카테고리이름/ 폴더 안의 소리 중 하나를 무작위로 재생한다.
/// 그 카테고리 폴더에 소리가 없으면 종이비행기(paper) 소리를 사용한다.
///
/// - 부딪힌 위치에서 들리도록 약간 입체음향으로 재생 (멀리 있어도 충분히 들림)
/// - 설정의 효과음 볼륨(SFX 믹서 그룹)을 따른다
/// - 매번 피치를 살짝 바꿔서 같은 소리가 반복돼도 덜 단조롭게
/// </summary>
public static class CrashSound
{
    private const string Root = "Sound/Crash/";

    private static readonly Dictionary<string, AudioClip[]> cache = new Dictionary<string, AudioClip[]>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cache.Clear();
    }

    /// <summary>카테고리의 충돌 효과음 재생</summary>
    public static void Play(string category, Vector3 position)
    {
        AudioClip clip = Pick(category);
        if (clip == null && category != PlaneCategory.Paper)
            clip = Pick(PlaneCategory.Paper);

        if (clip == null)
        {
            Debug.LogWarning($"[CrashSound] Resources/{Root}{category} 에 효과음이 없습니다.");
            return;
        }

        GameObject go = new GameObject("CrashSound");
        go.transform.position = position;

        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.clip = clip;
        source.volume = 1f;
        source.pitch = Random.Range(0.92f, 1.08f);
        source.spatialBlend = 0.6f;              // 위치감은 주되 멀어도 잘 들리게
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 1.5f;
        source.maxDistance = 20f;
        source.dopplerLevel = 0f;

        if (SFXManager.Instance != null && SFXManager.Instance.sfxMixerGroup != null)
            source.outputAudioMixerGroup = SFXManager.Instance.sfxMixerGroup;

        source.Play();
        Object.Destroy(go, clip.length / source.pitch + 0.2f);
    }

    /// <summary>카테고리 효과음 미리 불러오기 (첫 충돌 때 끊김 방지)</summary>
    public static void Preload(string category)
    {
        Load(category);
    }

    private static AudioClip Pick(string category)
    {
        AudioClip[] clips = Load(category);
        if (clips == null || clips.Length == 0)
            return null;

        return clips[Random.Range(0, clips.Length)];
    }

    private static AudioClip[] Load(string category)
    {
        if (string.IsNullOrEmpty(category))
            category = PlaneCategory.Paper;

        AudioClip[] clips;
        if (!cache.TryGetValue(category, out clips))
        {
            clips = Resources.LoadAll<AudioClip>(Root + category);
            cache[category] = clips;
        }

        return clips;
    }
}
