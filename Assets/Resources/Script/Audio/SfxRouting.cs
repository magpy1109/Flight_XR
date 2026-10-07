using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬에 놓인 효과음 AudioSource를 효과음(SFX) 믹서 그룹으로 연결한다.
///
/// 각 씬의 SoundManager(버튼 누르는 소리)는 믹서 그룹이 비어 있어서
/// 설정의 효과음 볼륨 / 음소거, 마스터 볼륨이 적용되지 않았다.
/// 씬이 열릴 때마다 믹서 그룹이 비어 있는 AudioSource를 찾아 SFX 그룹으로 보낸다. (씬 파일 수정 없음)
///
/// - 배경음(BGMManager)과 코드에서 만드는 효과음은 이미 그룹이 정해져 있어서 건드리지 않는다.
/// - SFX 그룹은 PersistentManagers 프리팹의 SFXManager에 연결된 것을 쓴다.
/// </summary>
public static class SfxRouting
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SFXManager.Instance == null || SFXManager.Instance.sfxMixerGroup == null)
        {
            Debug.LogWarning("[SfxRouting] SFX 믹서 그룹을 찾지 못해 효과음을 연결하지 못했습니다.");
            return;
        }

        AudioMixerGroup group = SFXManager.Instance.sfxMixerGroup;
        int count = 0;

        foreach (AudioSource source in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (source.outputAudioMixerGroup != null)
                continue;

            source.outputAudioMixerGroup = group;
            count++;
        }

        if (count > 0)
            Debug.Log($"[SfxRouting] {scene.name} : 효과음 AudioSource {count}개를 SFX 그룹으로 연결");
    }
}
