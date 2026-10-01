using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 스킨씬에 새 스킨(스텔스 폭격기, 9번)을 실행 시 추가한다. (씬 파일 수정 없음)
///
/// - 비행기 탭 2페이지 첫 칸을 폭격기 버튼으로 바꾸고, 같은 페이지의 "Coming Soon" 가림막과 나머지 빈 칸(중복 버튼)을 숨긴다.
/// - 버튼 그림 : Resources/PlaneModel/StealthBomberCard.png
/// - 가운데 3D 미리보기에 폭격기 모델을 추가 (윗면이 보이도록 살짝 기울인 채 천천히 회전)
/// - 해금 조건 : 최고 점수 500점 (SkinUnlockManager)
/// </summary>
public static class SkinSceneExtraSkins
{
    public const int BomberSkinID = 9;

    private const string CardPath = "PlaneModel/StealthBomberCard";
    private const string Title = "스텔스 폭격기";
    private const string Info = "레이더도 놓치는 검은 날개 (최고 점수 500점 달성 시 해금)";

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
            if (selector.gameObject.scene != scene || selector.gameObject.name != "FlightPanel")
                continue;

            try
            {
                Setup(selector);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[SkinSceneExtraSkins] 폭격기 스킨 추가 실패\n" + e);
            }
        }
    }

    private static void Setup(SkinSelector selector)
    {
        if (selector.planeModels == null || selector.planeModels.Length != 9)
            return;   // 이미 추가됐거나 구조가 다름

        GameObject preview = CreatePreview(selector.planeModels[0]);
        if (preview == null)
            return;

        var models = new GameObject[10];
        for (int i = 0; i < 9; i++)
            models[i] = selector.planeModels[i];
        models[BomberSkinID] = preview;
        selector.planeModels = models;

        SetupPageButton(selector.transform);

        Debug.Log("[SkinSceneExtraSkins] 스텔스 폭격기 스킨 추가");
    }

    // ---------- 2페이지 버튼 ----------

    private static void SetupPageButton(Transform panel)
    {
        Transform page = panel.Find("Page2");
        if (page == null)
        {
            Debug.LogWarning("[SkinSceneExtraSkins] Page2를 찾지 못했습니다.");
            return;
        }

        Sprite card = LoadCard();

        foreach (Transform child in page)
        {
            string n = child.name;

            // "Coming Soon" 가림막 / 클릭 막는 판
            if (n == "CommingSoon" || n == "ClickBlocker")
            {
                child.gameObject.SetActive(false);
                continue;
            }

            AutoSkinButton button = child.GetComponent<AutoSkinButton>();
            if (button != null)
            {
                if (button.skinID == BomberSkinID)
                {
                    button.skinTitle = Title;
                    button.skinInfo = Info;
                    button.bigSkinSprite = null;

                    Transform image = child.Find("Image");
                    Image img = image != null ? image.GetComponent<Image>() : null;
                    if (img != null && card != null)
                    {
                        img.sprite = card;
                        img.preserveAspect = true;
                    }
                }
                else
                {
                    child.gameObject.SetActive(false);   // 1페이지와 같은 중복 버튼
                }
                continue;
            }

            // 버튼 아래 이름
            if (n == "SkinName_Text")
            {
                TMP_Text text = child.GetComponent<TMP_Text>();
                if (text != null)
                {
                    text.text = Title;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                }
            }
            else if (n.StartsWith("SkinName_Text"))
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private static Sprite LoadCard()
    {
        Texture2D tex = Resources.Load<Texture2D>(CardPath);
        if (tex == null)
        {
            Debug.LogWarning($"[SkinSceneExtraSkins] Resources/{CardPath} 그림을 찾지 못했습니다.");
            return null;
        }

        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    // ---------- 3D 미리보기 ----------

    private static GameObject CreatePreview(GameObject reference)
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
        GameObject pivot = new GameObject("StealthBomberPreview");
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
        GameObject model = PlaneModels.Create(PlaneModels.BomberMeshPath, spinner.transform, size * 1.35f, out filter);
        if (model == null)
        {
            Object.Destroy(pivot);
            return null;
        }

        model.layer = reference.layer;

        // 스킨씬 미리보기 모델과 같이 화면 밖으로 잘리지 않게 (MeshFixer와 같은 방식)
        Mesh copy = Object.Instantiate(filter.sharedMesh);
        copy.bounds = new Bounds(copy.bounds.center, Vector3.one * 100000f);
        filter.sharedMesh = copy;

        // 처음 보이는 방향 : 기수가 비스듬히 앞쪽
        spinner.transform.localRotation = Quaternion.Euler(0f, 200f, 0f);

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
