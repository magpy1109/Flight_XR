using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// BGM 카테고리. 게임 내 BGM과 일반(로비/메뉴) BGM을 분리 관리.
/// 볼륨은 AudioSettingsManager 의 BGM 볼륨 하나로 통합 제어됨 (같은 믹서 그룹 사용).
/// </summary>
public enum BGMCategory
{
    General,    // 로비, 메인메뉴 등에서 재생되는 일반 BGM
    Game        // 실제 플레이 중 재생되는 게임 내 BGM
}

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [Header("[ Audio Mixer Group ]")]
    [Tooltip("AudioSettingsManager 의 BGMVolume 파라미터가 걸려있는 믹서 그룹")]
    public AudioMixerGroup bgmMixerGroup;

    [Header("[ BGM Playlists ]")]
    [Tooltip("메인메뉴, 로비 등에서 재생되는 일반 BGM 목록 (여러 개 가능)")]
    public List<AudioClip> generalBGMList = new List<AudioClip>();

    [Tooltip("실제 게임 플레이 중 재생되는 BGM 목록 (여러 개 가능)")]
    public List<AudioClip> gameBGMList = new List<AudioClip>();

    [Header("[ Playback Settings ]")]
    public float crossfadeDuration = 1.5f;
    [Tooltip("체크하면 리스트 안에서 랜덤 순서로 재생 (곡이 끝나면 다음 곡도 자동으로 랜덤 선곡)")]
    public bool shufflePlaylist = true;

    [Header("[ Auto Play ]")]
    [Tooltip("씬 시작 시 자동으로 재생을 시작할지 여부")]
    public bool autoPlayOnStart = false;
    [Tooltip("자동 재생 시 어떤 카테고리를 틀지 선택")]
    public BGMCategory autoPlayCategory = BGMCategory.General;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private AudioSource activeSource;
    private AudioSource inactiveSource;

    private BGMCategory currentCategory = BGMCategory.General;
    private List<AudioClip> currentPlaylist;
    private int currentIndex = -1;

    private Coroutine autoAdvanceRoutine;
    private Coroutine crossfadeRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();
        foreach (var src in new[] { sourceA, sourceB })
        {
            src.loop = false;
            src.playOnAwake = false;
            if (bgmMixerGroup != null) src.outputAudioMixerGroup = bgmMixerGroup;
        }
        activeSource = sourceA;
        inactiveSource = sourceB;
    }

    void Start()
    {
        if (autoPlayOnStart)
        {
            PlayCategory(autoPlayCategory, -1);
        }
    }

    /// <summary>일반(로비/메뉴) BGM 재생. index를 안 주면 자동 선곡.</summary>
    public void PlayGeneralBGM(int index = -1) => PlayCategory(BGMCategory.General, index);

    /// <summary>게임 내 BGM 재생. index를 안 주면 자동 선곡.</summary>
    public void PlayGameBGM(int index = -1) => PlayCategory(BGMCategory.Game, index);

    private void PlayCategory(BGMCategory category, int index)
    {
        List<AudioClip> list = category == BGMCategory.Game ? gameBGMList : generalBGMList;
        if (list == null || list.Count == 0)
        {
            Debug.LogWarning($"[BGMManager] {category} BGM 목록이 비어있습니다. Inspector에서 클립을 추가해주세요.");
            return;
        }

        currentCategory = category;
        currentPlaylist = list;
        currentIndex = (index >= 0 && index < list.Count) ? index : GetNextIndex();

        AudioClip clip = currentPlaylist[currentIndex];
        StartCrossfade(clip);

        if (autoAdvanceRoutine != null) StopCoroutine(autoAdvanceRoutine);
        autoAdvanceRoutine = StartCoroutine(AutoAdvanceRoutine(clip.length));
    }

    private int GetNextIndex()
    {
        if (currentPlaylist.Count == 1) return 0;

        if (shufflePlaylist)
        {
            int next;
            do { next = Random.Range(0, currentPlaylist.Count); }
            while (next == currentIndex);
            return next;
        }

        return (currentIndex + 1) % currentPlaylist.Count;
    }

    private IEnumerator AutoAdvanceRoutine(float clipLength)
    {
        float wait = Mathf.Max(0.1f, clipLength - crossfadeDuration);
        yield return new WaitForSeconds(wait);
        PlayCategory(currentCategory, GetNextIndex());
    }

    private void StartCrossfade(AudioClip clip)
    {
        if (crossfadeRoutine != null) StopCoroutine(crossfadeRoutine);
        crossfadeRoutine = StartCoroutine(CrossfadeRoutine(clip));
    }

    private IEnumerator CrossfadeRoutine(AudioClip clip)
    {
        inactiveSource.clip = clip;
        inactiveSource.volume = 0f;
        inactiveSource.Play();

        float t = 0f;
        float startVolActive = activeSource.volume;

        while (t < crossfadeDuration)
        {
            t += Time.deltaTime;
            float ratio = t / crossfadeDuration;
            inactiveSource.volume = Mathf.Lerp(0f, 1f, ratio);
            activeSource.volume = Mathf.Lerp(startVolActive, 0f, ratio);
            yield return null;
        }

        activeSource.Stop();
        activeSource.volume = 1f;

        // 활성/비활성 소스 스왑
        var temp = activeSource;
        activeSource = inactiveSource;
        inactiveSource = temp;
    }

    /// <summary>BGM 완전 정지 (씬 전환, 일시정지 메뉴 등에서 사용)</summary>
    public void Stop()
    {
        if (autoAdvanceRoutine != null) StopCoroutine(autoAdvanceRoutine);
        if (crossfadeRoutine != null) StopCoroutine(crossfadeRoutine);
        sourceA.Stop();
        sourceB.Stop();
    }
}