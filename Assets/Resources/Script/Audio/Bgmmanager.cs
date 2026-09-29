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

    // 설정 저장 키 (설정씬의 BGM 선택 / 반복 재생)
    private const string KEY_BGM_INDEX = "Setting_BGMIndex";   // -2 = 전체 랜덤 재생(기본), 0 이상 = 곡 번호

    /// <summary>BGM 선택값 : 전체 곡을 랜덤 순서로 재생 (기본값)</summary>
    public const int SelectAllShuffle = -2;

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
            // 일시정지(AudioListener.pause) 중에도 BGM은 계속 재생
            src.ignoreListenerPause = true;
            if (bgmMixerGroup != null) src.outputAudioMixerGroup = bgmMixerGroup;
        }
        activeSource = sourceA;
        inactiveSource = sourceB;
    }

    void Start()
    {
        if (autoPlayOnStart)
        {
            // 일반 BGM은 설정에서 고른 곡이 있으면 그 곡부터 재생
            int startIndex = autoPlayCategory == BGMCategory.General ? SavedGeneralIndex : -1;
            PlayCategory(autoPlayCategory, startIndex);
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

        if (UseShuffle)
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
        // 일시정지(Time.timeScale = 0) 중에도 곡이 끝나면 다음 곡으로 넘어가도록 실제 시간 사용
        yield return new WaitForSecondsRealtime(wait);

        // 설정에서 곡을 골랐으면 그 곡 반복, 전체 랜덤 재생이면 다음 곡
        int next = IsSingleSongSelected ? currentIndex : GetNextIndex();
        PlayCategory(currentCategory, next);
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
            t += Time.unscaledDeltaTime;
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

    // ---------- 설정씬 연동 (BGM 선택 / 반복 재생) ----------

    /// <summary>
    /// 설정에 저장된 BGM 선택값.
    /// SelectAllShuffle(-2) = 전체 랜덤 재생(기본), 0 이상 = 곡 번호
    /// </summary>
    public int SavedGeneralSelection
    {
        get
        {
            int value = PlayerPrefs.GetInt(KEY_BGM_INDEX, SelectAllShuffle);
            return (value >= 0 && value < generalBGMList.Count) ? value : SelectAllShuffle;
        }
    }

    /// <summary>설정에서 고른 곡 번호. 전체 랜덤 재생이면 -1</summary>
    public int SavedGeneralIndex => SavedGeneralSelection >= 0 ? SavedGeneralSelection : -1;

    /// <summary>다음 곡을 랜덤으로 고를지 (일반 BGM은 항상 랜덤, 게임 BGM은 Inspector 값)</summary>
    private bool UseShuffle =>
        currentCategory == BGMCategory.General || shufflePlaylist;

    /// <summary>일반 BGM에서 특정 곡을 골라 그 곡만 반복 중인지</summary>
    private bool IsSingleSongSelected =>
        currentCategory == BGMCategory.General && SavedGeneralSelection >= 0;

    /// <summary>일반 BGM 곡 이름 목록 (설정 드롭다운용)</summary>
    public List<string> GetGeneralBGMNames()
    {
        var names = new List<string>();
        foreach (var clip in generalBGMList)
            names.Add(clip != null ? clip.name : "(비어 있음)");
        return names;
    }

    /// <summary>
    /// 일반 BGM 선택.
    /// SelectAllShuffle 이면 전체 랜덤 재생 (지금 곡은 끊지 않고 다음 곡부터 적용),
    /// 곡 번호면 즉시 그 곡으로 바뀌고, 그 곡만 계속 반복한다.
    /// </summary>
    public void SelectGeneralBGM(int index)
    {
        if (index < 0 || index >= generalBGMList.Count)
            index = SelectAllShuffle;

        PlayerPrefs.SetInt(KEY_BGM_INDEX, index);
        PlayerPrefs.Save();

        // 전체 랜덤 재생 : 이미 일반 BGM이 나오고 있으면 끊지 않고, 다음 곡부터 적용
        if (index < 0)
        {
            bool playing = currentCategory == BGMCategory.General &&
                           activeSource != null && activeSource.isPlaying;

            if (!playing)
                PlayCategory(BGMCategory.General, -1);

            return;
        }

        // 이미 그 곡이 재생 중이면 다시 시작하지 않음
        if (index >= 0 &&
            currentCategory == BGMCategory.General &&
            currentIndex == index &&
            activeSource != null && activeSource.isPlaying)
        {
            return;
        }

        PlayCategory(BGMCategory.General, index);
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