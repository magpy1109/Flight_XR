using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 스킨씬의 비행기 탭을 실행 시 채운다. (씬 파일 수정 없음)
///
/// 스킨 순서 : 0번 기본 종이비행기(클래식 화이트) → 9번 스텔스 폭격기 → 10번부터 텍스처 모델 스킨(PlaneSkins 목록 순서)
/// - 한 페이지 9칸. 1페이지 첫 칸(기본 스킨)은 씬 그대로 두고, 나머지 칸의 버튼 번호 / 이름 / 그림을 차례로 바꾼다.
/// - 스킨이 없는 칸의 버튼과 이름은 숨긴다. 스킨이 놓인 2페이지 이후는 "Coming Soon" 가림막을 끈다.
/// - 버튼 그림 : Resources/PlaneModel/StealthBomberCard.png, Resources/PlaneModel/이름/이름_Card.png
/// - 가운데 3D 미리보기에 스킨마다 모델을 추가 (윗면이 보이도록 살짝 기울인 채 천천히 회전)
///   미리보기 모델은 종이비행기 미리보기와 같은 부모(Skin_Flight) 아래에 둔다 → 트레일 탭으로 가면 함께 숨겨짐
/// - 해금 조건 : SkinUnlockManager (10번부터는 PlaneSkins 목록)
///
/// 트레일 탭도 같은 방식으로 TrailCatalog 목록으로 채운다.
/// - 첫 칸 "스킨별 기본 트레일", 기존 색 트레일 9종, 스킨별 트레일 순서
/// - 코드로 만드는 트레일(TrailEffects)은 기존 트레일 미리보기와 같은 자리에 미리보기를 만든다.
/// </summary>
public static class SkinSceneExtraSkins
{
    public const int BomberSkinID = PlaneSkinState.BomberSkin;

    private const int SlotsPerPage = 9;

    // 버튼 누르는 소리 (1페이지 버튼이 씬에서 쓰는 것과 같은 소리)
    private const string ClickSoundPath = "Sound/Click";

    private const string BomberCardPath = "PlaneModel/StealthBomberCard";
    private const string BomberTitle = "스텔스 폭격기";
    private const string BomberInfo = "레이더도 놓치는 검은 날개 (최고 점수 500점 달성 시 해금)";

    // 스킨씬 한 칸에 놓을 스킨
    private class Entry
    {
        public int id;
        public string title;
        public string info;
        public string cardPath;    // 버튼 그림 (없으면 동그라미에 tint 색만 입힘 : 트레일)
        public Color tint = Color.white;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // sceneLoaded는 Awake / OnEnable 다음, Start 전에 호출된다 → SkinSelector.Start 전에 모델 목록을 늘릴 수 있음
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (SkinSelector selector in Object.FindObjectsByType<SkinSelector>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (selector.gameObject.scene != scene)
                continue;

            try
            {
                if (selector.gameObject.name == "FlightPanel")
                    Setup(selector);
                else if (selector.gameObject.name == "TrailPanel")
                    SetupTrails(selector);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[SkinSceneExtraSkins] 스킨 목록 준비 실패\n" + e);
            }
        }
    }

    private static void Setup(SkinSelector selector)
    {
        GameObject[] old = selector.planeModels;
        if (old == null || old.Length == 0 || old.Length > BomberSkinID)
            return;   // 이미 준비됐거나 구조가 다름

        GameObject reference = old[0];

        // 스킨 번호 그대로 쓰도록 0 ~ 가장 큰 번호 칸을 만들고, 삭제된 색 스킨(1~8번) 칸은 비워 둔다.
        var models = new GameObject[PlaneSkinState.SkinCount];
        models[0] = reference;
        for (int i = 1; i < old.Length; i++)
        {
            if (old[i] != null)
                old[i].SetActive(false);
        }

        Transform group = CreateGroup(reference.transform.parent);

        // 스킨씬에 놓을 순서 (첫 칸 = 기본 스킨 : 씬 그대로)
        var entries = new List<Entry>();
        entries.Add(null);

        GameObject bomber = CreatePreview(reference, null, group);
        if (bomber != null)
        {
            models[BomberSkinID] = bomber;
            entries.Add(new Entry { id = BomberSkinID, title = BomberTitle, info = BomberInfo, cardPath = BomberCardPath });
        }

        foreach (PlaneSkins.Skin skin in PlaneSkins.All)
        {
            if (skin.id <= BomberSkinID || skin.id >= models.Length)
                continue;

            GameObject preview = CreatePreview(reference, skin, group);
            if (preview == null)
                continue;

            models[skin.id] = preview;
            entries.Add(new Entry { id = skin.id, title = skin.title, info = skin.info, cardPath = skin.CardPath });
        }

        selector.planeModels = models;

        for (int page = 0; page * SlotsPerPage < entries.Count; page++)
        {
            if (!SetupPage(selector.transform, page, entries, null))
                break;
        }

        Debug.Log($"[SkinSceneExtraSkins] 비행기 스킨 {entries.Count}개 준비");
    }

    // ---------- 트레일 탭 ----------

    /// <summary>
    /// 트레일 탭을 TrailCatalog 목록으로 채운다.
    /// 첫 칸 = 스킨별 기본 트레일, 그다음 기존 색 트레일 9종, 그 뒤로 스킨별 트레일.
    /// </summary>
    private static void SetupTrails(SkinSelector selector)
    {
        GameObject[] old = selector.planeModels;
        if (old == null || old.Length == 0 || old.Length > TrailCatalog.BomberId || old[0] == null)
            return;   // 이미 준비됐거나 구조가 다름

        GameObject reference = old[0];

        // 씬 버튼에 적혀 있던 이름 / 설명 (기존 색 트레일은 그대로 쓴다)
        var sceneTitles = new Dictionary<int, string>();
        var sceneInfos = new Dictionary<int, string>();
        Sprite circle = null;

        foreach (AutoSkinButton button in selector.GetComponentsInChildren<AutoSkinButton>(true))
        {
            if (!sceneTitles.ContainsKey(button.skinID))
            {
                sceneTitles[button.skinID] = button.skinTitle;
                sceneInfos[button.skinID] = button.skinInfo;
            }

            if (circle == null)
            {
                Transform image = button.transform.Find("Image");
                Image img = image != null ? image.GetComponent<Image>() : null;
                if (img != null)
                    circle = img.sprite;
            }
        }

        var models = new GameObject[TrailCatalog.MaxId + 1];
        var entries = new List<Entry>();

        foreach (TrailCatalog.Trail trail in TrailCatalog.All)
        {
            GameObject preview;
            string title = trail.title;
            string info = trail.info;

            if (trail.id == TrailCatalog.AutoId)
            {
                preview = CreateAutoTrailPreview(reference, models);
            }
            else if (trail.prefabIndex >= 0)
            {
                preview = trail.prefabIndex < old.Length ? old[trail.prefabIndex] : null;

                string sceneText;
                if (sceneTitles.TryGetValue(trail.prefabIndex, out sceneText) && !string.IsNullOrEmpty(sceneText))
                    title = sceneText;
                if (sceneInfos.TryGetValue(trail.prefabIndex, out sceneText) && !string.IsNullOrEmpty(sceneText))
                    info = sceneText;
            }
            else
            {
                preview = CreateTrailPreview(reference, trail);
            }

            if (preview == null)
                continue;

            models[trail.id] = preview;
            entries.Add(new Entry { id = trail.id, title = title, info = info, cardPath = trail.iconPath, tint = trail.iconColor });
        }

        selector.planeModels = models;

        for (int page = 0; page * SlotsPerPage < entries.Count; page++)
        {
            if (!SetupPage(selector.transform, page, entries, circle))
                break;
        }

        // 처음 보이는 이름 / 설명 = 첫 칸
        if (entries.Count > 0)
        {
            if (selector.titleText != null)
                selector.titleText.text = entries[0].title;
            if (selector.infoText != null)
                selector.infoText.text = entries[0].info;
        }

        Debug.Log($"[SkinSceneExtraSkins] 트레일 {entries.Count}개 준비");
    }

    /// <summary>코드로 만드는 트레일의 미리보기 (기존 트레일 미리보기와 같은 자리 / 같은 줄기 위치)</summary>
    private static GameObject CreateTrailPreview(GameObject reference, TrailCatalog.Trail trail)
    {
        GameObject preview = NewPreviewObject(reference, trail.key + "TrailPreview");

        int emitters = 0;
        foreach (Transform source in reference.transform)
        {
            GameObject emitter = new GameObject(source.name);
            emitter.layer = source.gameObject.layer;
            emitter.transform.SetParent(preview.transform, false);
            emitter.transform.localPosition = source.localPosition;
            emitter.transform.localRotation = source.localRotation;
            emitter.transform.localScale = source.localScale;

            TrailEffects.Build(emitter.transform, trail.id);
            emitters++;
        }

        if (emitters == 0)
            TrailEffects.Build(preview.transform, trail.id);

        return preview;
    }

    /// <summary>"스킨별 기본 트레일" 미리보기 : 켜질 때 지금 장착한 비행기의 트레일 미리보기를 대신 켠다</summary>
    private static GameObject CreateAutoTrailPreview(GameObject reference, GameObject[] models)
    {
        GameObject preview = NewPreviewObject(reference, "AutoTrailPreview");

        AutoTrailPreview auto = preview.AddComponent<AutoTrailPreview>();
        auto.resolve = () =>
        {
            int id = TrailCatalog.ForSkin(PlaneSkinState.EquippedPlaneIndex);
            return id >= 0 && id < models.Length ? models[id] : null;
        };

        return preview;
    }

    /// <summary>기존 미리보기(reference)와 같은 부모 / 위치 / 기울기의 빈 오브젝트 (꺼진 채로)</summary>
    private static GameObject NewPreviewObject(GameObject reference, string name)
    {
        GameObject go = new GameObject(name);
        go.layer = reference.layer;
        go.SetActive(false);
        go.transform.SetParent(reference.transform.parent, false);
        go.transform.localPosition = reference.transform.localPosition;
        go.transform.localRotation = reference.transform.localRotation;
        go.transform.localScale = reference.transform.localScale;
        return go;
    }

    // ---------- 페이지 버튼 ----------

    /// <summary>한 페이지(9칸)의 버튼 / 이름을 스킨 목록에 맞춘다. 페이지를 못 찾으면 false</summary>
    private static bool SetupPage(Transform panel, int pageIndex, List<Entry> entries, Sprite tintSprite)
    {
        // 비행기 탭은 "Page1", 트레일 탭은 "Page1 (1)" 같은 이름
        Transform page = panel.Find("Page" + (pageIndex + 1));
        if (page == null)
            page = panel.Find("Page" + (pageIndex + 1) + " (1)");
        if (page == null)
        {
            Debug.LogWarning($"[SkinSceneExtraSkins] Page{pageIndex + 1}을 찾지 못해 일부 스킨을 놓지 못했습니다.");
            return false;
        }

        // 버튼 : 씬에 놓인 순서 = 칸 순서 (왼쪽 위부터)
        var buttons = new List<AutoSkinButton>();
        foreach (Transform child in page)
        {
            AutoSkinButton button = child.GetComponent<AutoSkinButton>();
            if (button != null)
                buttons.Add(button);
        }

        for (int slot = 0; slot < buttons.Count; slot++)
        {
            AutoSkinButton button = buttons[slot];
            TMP_Text label = FindLabel(page, slot);

            int index = pageIndex * SlotsPerPage + slot;
            if (index >= entries.Count)
            {
                // 스킨이 없는 칸
                button.gameObject.SetActive(false);
                if (label != null)
                    label.gameObject.SetActive(false);
                continue;
            }

            Entry entry = entries[index];
            if (entry != null)
            {
                button.skinID = entry.id;
                button.skinTitle = entry.title;
                button.skinInfo = entry.info;
                button.bigSkinSprite = null;

                Transform image = button.transform.Find("Image");
                Image img = image != null ? image.GetComponent<Image>() : null;
                if (img != null)
                {
                    Sprite card = string.IsNullOrEmpty(entry.cardPath) ? null : LoadCard(entry.cardPath);
                    if (card != null)
                    {
                        img.sprite = card;
                        img.color = Color.white;
                        img.preserveAspect = true;
                    }
                    else if (tintSprite != null)
                    {
                        // 그림이 없는 트레일 : 동그라미에 색만 입힘
                        img.sprite = tintSprite;
                        img.color = entry.tint;
                    }
                }

                if (label != null)
                    label.text = entry.title;
            }

            CenterLabel(label, button.transform as RectTransform);
        }

        AddMissingClickSounds(panel, buttons);

        // 2페이지 이후 : "Coming Soon" 가림막 / 클릭 막는 판 끄기
        if (pageIndex > 0)
        {
            foreach (Transform child in page)
            {
                if (child.name == "CommingSoon" || child.name == "ClickBlocker")
                    child.gameObject.SetActive(false);
            }
        }

        return true;
    }

    /// <summary>
    /// 누르는 소리가 연결되지 않은 버튼에 소리를 붙인다.
    /// (씬에서 1페이지 버튼만 소리가 연결돼 있고, 2페이지 이후 버튼은 "Coming Soon"이라 비어 있었음)
    /// </summary>
    private static void AddMissingClickSounds(Transform panel, List<AutoSkinButton> buttons)
    {
        AudioSource source = FindClickSource(panel);
        AudioClip clip = Resources.Load<AudioClip>(ClickSoundPath);
        if (source == null || clip == null)
            return;

        foreach (AutoSkinButton skinButton in buttons)
        {
            Button button = skinButton.GetComponent<Button>();
            if (button == null || HasClickSound(button))
                continue;

            button.onClick.AddListener(() =>
            {
                if (source != null)
                    source.PlayOneShot(clip);
            });
        }
    }

    /// <summary>씬에서 이미 누르는 소리가 연결된 버튼인지</summary>
    private static bool HasClickSound(Button button)
    {
        int count = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            if (button.onClick.GetPersistentTarget(i) is AudioSource)
                return true;
        }

        return false;
    }

    /// <summary>1페이지 버튼이 소리를 내는 데 쓰는 AudioSource (씬의 SoundManager)</summary>
    private static AudioSource FindClickSource(Transform panel)
    {
        foreach (Button button in panel.GetComponentsInChildren<Button>(true))
        {
            int count = button.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                AudioSource source = button.onClick.GetPersistentTarget(i) as AudioSource;
                if (source != null)
                    return source;
            }
        }

        return null;
    }

    /// <summary>칸 번호의 이름 오브젝트 (첫 칸 "SkinName_Text", 그다음부터 "SkinName_Text (1)" ...)</summary>
    private static TMP_Text FindLabel(Transform page, int slot)
    {
        Transform t = page.Find(slot == 0 ? "SkinName_Text" : $"SkinName_Text ({slot})");
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    /// <summary>이름을 버튼 바로 아래 가운데에 맞춘다 (이름 길이가 달라도 어긋나지 않게)</summary>
    private static void CenterLabel(TMP_Text label, RectTransform button)
    {
        if (label == null || button == null)
            return;

        RectTransform rect = label.rectTransform;
        rect.anchoredPosition = new Vector2(button.anchoredPosition.x, rect.anchoredPosition.y);

        label.alignment = TextAlignmentOptions.Top;
        label.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private static Sprite LoadCard(string path)
    {
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex == null)
        {
            Debug.LogWarning($"[SkinSceneExtraSkins] Resources/{path} 그림을 찾지 못했습니다.");
            return null;
        }

        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    // ---------- 3D 미리보기 ----------

    /// <summary>
    /// 미리보기 모델들을 담을 묶음. 종이비행기 미리보기의 부모(Skin_Flight) 아래에 만든다.
    /// - 탭 버튼이 Skin_Flight를 켜고 끄므로, 그 아래에 있어야 트레일 탭에서 함께 숨겨진다.
    /// - Skin_Flight는 스스로 돌고 있어서(ObjectRotator) 묶음은 반대로 돌려 제자리에 있게 한다.
    ///   (모델마다 따로 기울여서 돌리기 때문)
    /// </summary>
    private static Transform CreateGroup(Transform parent)
    {
        GameObject group = new GameObject("ExtraSkinPreviews");

        if (parent == null)
            return group.transform;

        group.layer = parent.gameObject.layer;
        group.transform.SetParent(parent, false);
        group.transform.localPosition = Vector3.zero;
        group.transform.rotation = Quaternion.identity;

        ObjectRotator parentRotator = parent.GetComponent<ObjectRotator>();
        if (parentRotator != null && parentRotator.enabled)
        {
            ObjectRotator counter = group.AddComponent<ObjectRotator>();
            counter.rotationSpeed = -parentRotator.rotationSpeed;
        }

        return group.transform;
    }

    /// <summary>미리보기 모델 만들기. skin이 null이면 스텔스 폭격기</summary>
    private static GameObject CreatePreview(GameObject reference, PlaneSkins.Skin skin, Transform group)
    {
        if (reference == null)
            return null;

        Transform refTransform = reference.transform;

        // 종이비행기 미리보기 크기 (회전 무관하게 가장 긴 변)
        float size = 1f;
        Vector3 center = refTransform.position;
        MeshFilter refFilter = reference.GetComponentInChildren<MeshFilter>(true);
        if (refFilter != null && refFilter.sharedMesh != null)
        {
            Vector3 s = Vector3.Scale(refFilter.sharedMesh.bounds.size, refFilter.transform.lossyScale);
            size = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            center = refFilter.transform.TransformPoint(refFilter.sharedMesh.bounds.center);
        }

        // 기울어진 받침(pivot) 위에서 모델만 회전 → 윗면이 항상 카메라 쪽으로 보임
        string name = skin != null ? skin.key : "StealthBomber";
        GameObject pivot = new GameObject(name + "Preview");
        pivot.layer = reference.layer;
        pivot.SetActive(false);
        pivot.transform.position = center;

        Camera cam = FindPreviewCamera(center);
        Vector3 up = Vector3.up;
        if (cam != null)
        {
            Vector3 toCam = (cam.transform.position - center).normalized;
            up = (Vector3.up + toCam * 0.6f).normalized;
        }
        pivot.transform.rotation = Quaternion.FromToRotation(Vector3.up, up);

        GameObject spinner = new GameObject("Spin");
        spinner.layer = reference.layer;
        spinner.transform.SetParent(pivot.transform, false);
        ObjectRotator rotator = spinner.AddComponent<ObjectRotator>();
        rotator.rotationSpeed = new Vector3(0f, 30f, 0f);

        MeshFilter filter;
        GameObject model = skin != null
            ? PlaneModels.CreateTextured(skin, spinner.transform, size * 1.2f, out filter)
            : PlaneModels.Create(PlaneModels.BomberMeshPath, spinner.transform, size * 1.35f, out filter);
        if (model == null)
        {
            Object.Destroy(pivot);
            return null;
        }

        model.layer = reference.layer;

        // 조명 / 그림자 설정은 종이비행기 미리보기와 똑같이 맞춘다. (메시는 게임에서 쓰는 것 그대로 사용)
        MeshRenderer refRenderer = reference.GetComponentInChildren<MeshRenderer>(true);
        MeshRenderer renderer = model.GetComponent<MeshRenderer>();
        if (refRenderer != null && renderer != null)
        {
            renderer.shadowCastingMode = refRenderer.shadowCastingMode;
            renderer.receiveShadows = refRenderer.receiveShadows;
            renderer.lightProbeUsage = refRenderer.lightProbeUsage;
            renderer.reflectionProbeUsage = refRenderer.reflectionProbeUsage;
            renderer.renderingLayerMask = refRenderer.renderingLayerMask;
        }

        // 처음 보이는 방향 : 기수가 비스듬히 앞쪽
        spinner.transform.localRotation = Quaternion.Euler(0f, 200f, 0f);

        // 종이비행기 미리보기와 같은 묶음 아래로 (위치 / 기울기는 그대로)
        if (group != null)
            pivot.transform.SetParent(group, true);

        return pivot;
    }

    private static Camera FindPreviewCamera(Vector3 target)
    {
        Camera best = null;
        float bestDistance = float.MaxValue;

        foreach (Camera cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (cam.targetTexture == null)
                continue;

            float d = Vector3.Distance(cam.transform.position, target);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = cam;
            }
        }

        return best;
    }
}
