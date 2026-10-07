using UnityEngine;

/// <summary>
/// 게임 비행기의 겉모습 (종이비행기 모델 + 장착한 스킨 색 + 장착한 트레일).
///
/// PlaneLauncher가 비행기를 만들 때 PlaneAppearance.Setup(plane)을 호출한다.
/// - 기존 네모(Cube) 표시는 끄고, 종이비행기 모델을 붙인다.
///   모델 데이터(Resources/PlaneModel/PaperPlaneMesh.bytes)는 스킨씬과 같은 원본(3D_FLIGHT.fbx, 약 195만 삼각형)을
///   Quest에서 가볍게 돌도록 약 1만2천 삼각형으로 줄인 것. (기수 +Z, 위 +Y, 길이 1)
/// - 스킨 : 0번 기본 종이비행기(클래식 화이트), 9번 스텔스 폭격기, 10번부터 텍스처 모델 스킨(PlaneSkins)
///   (1~8번 색 스킨은 삭제됨)
/// - 10번부터의 스킨은 종이비행기 대신 그 스킨의 모델 + 색 텍스처(PlaneModels.CreateTextured)
/// - 트레일을 "스킨별 기본 트레일"로 두면 스킨에 어울리는 트레일이 붙고, 다른 트레일을 고르면 그 트레일이 붙는다. (TrailCatalog)
/// - 9번 스킨(스텔스 폭격기)은 종이비행기 대신 폭격기 모델(PlaneModels) + 전용 비행운(BomberEffects)
/// - 트레일 : 스킨씬의 트레일 9종을 그대로 옮긴 Resources/PlaneModel/PlaneTrails 프리팹 사용
/// - 비행 중 방향을 틀면 기울고, 오르내리면 기수가 들리고 숙여진다. (겉모습만, 물리는 그대로)
/// - 추락하면 트레일이 멈춘다.
/// </summary>
public class PlaneAppearance : MonoBehaviour
{
    private const string MeshPath = "PlaneModel/PaperPlaneMesh";
    private const string MaterialPath = "PlaneModel/PlaneSkin_Mat";
    private const string TrailPath = "PlaneModel/PlaneTrails";

    [Tooltip("비행기 길이 (m)")]
    public float planeLength = 0.18f;

    [Tooltip("방향을 틀 때 최대 기울기 (도)")]
    public float maxBank = 30f;

    [Tooltip("오르내릴 때 최대 기수 각도 (도)")]
    public float maxPitch = 25f;

    [Tooltip("기울기가 따라가는 빠르기")]
    public float tiltSpeed = 6f;

    private Transform visual;
    private Rigidbody rb;
    private ParticleSystem[] trails;
    private TrailRenderer[] lineTrails;   // 폭격기 비행운
    private bool trailsStopped;

    [Tooltip("스텔스 폭격기 날개폭 (m)")]
    public float bomberSpan = 0.24f;

    private float bank;
    private float pitch;

    /// <summary>기울기용 모델 부모 (충돌 효과에서 사용)</summary>
    public Transform Visual => visual;

    /// <summary>종이비행기 모델 메시 (충돌 시 찌그러짐 애니메이션에 사용)</summary>
    public MeshFilter ModelFilter { get; private set; }

    /// <summary>비행기 종류 (충돌 효과음 / 애니메이션 구분, PlaneCategory 참고)</summary>
    public string Category { get; private set; } = PlaneCategory.Paper;

    // ---------- 설치 ----------

    /// <summary>비행기에 모델 / 스킨 / 트레일 적용</summary>
    public static PlaneAppearance Setup(GameObject plane)
    {
        if (plane == null)
            return null;

        PlaneAppearance appearance = plane.GetComponent<PlaneAppearance>();
        if (appearance == null)
            appearance = plane.AddComponent<PlaneAppearance>();

        appearance.Build(PlaneSkinState.EquippedPlaneIndex, PlaneSkinState.EquippedTrailIndex);
        return appearance;
    }

    private void Build(int skinIndex, int trailIndex)
    {
        rb = GetComponent<Rigidbody>();

        // 비행기 종류 + 충돌 효과음 미리 불러오기 (첫 충돌 때 끊기지 않게)
        Category = PlaneCategory.OfSkin(skinIndex);
        CrashSound.Preload(Category);

        // 실제로 붙일 트레일 ("스킨별 기본 트레일"이면 이 스킨에 어울리는 트레일)
        int trailId = TrailCatalog.Resolve(trailIndex, skinIndex);

        // 텍스처 모델 스킨 (10번부터)
        PlaneSkins.Skin skin = PlaneSkins.Get(skinIndex);
        if (skin != null)
        {
            if (BuildModelSkin(skin, trailId))
            {
                Debug.Log($"[PlaneAppearance] 스킨 {skinIndex} ({skin.title}) / 트레일 {trailIndex} → {trailId} 적용");
                return;
            }

            // 모델을 못 불러오면 종이비행기로 대신 표시
            Category = PlaneCategory.Paper;
            CrashSound.Preload(Category);
        }

        // 종이비행기가 아닌 모델 (스텔스 폭격기)
        if (Category == PlaneCategory.Bomber && BuildBomber(trailId))
        {
            Debug.Log($"[PlaneAppearance] 스킨 {skinIndex} ({PlaneSkinState.SkinName(skinIndex)}) / 트레일 {trailIndex} → {trailId} 적용");
            return;
        }

        Mesh mesh = LoadMesh();
        if (mesh == null)
        {
            Debug.LogError($"[PlaneAppearance] Resources/{MeshPath} 모델 데이터를 찾지 못해 기존 모양을 사용합니다.");
            return;
        }

        // 1) 기존 네모 표시 끄기 + 루트 크기를 1로 (충돌 크기는 아래에서 모델에 맞춤)
        MeshRenderer cubeRenderer = GetComponent<MeshRenderer>();
        if (cubeRenderer != null)
            cubeRenderer.enabled = false;

        transform.localScale = Vector3.one;

        // 2) 모델 (기울기용 Visual 아래)
        GameObject visualObject = new GameObject("Visual");
        visual = visualObject.transform;
        visual.SetParent(transform, false);

        GameObject model = new GameObject("PaperPlane", typeof(MeshFilter), typeof(MeshRenderer));
        model.transform.SetParent(visual, false);
        ModelFilter = model.GetComponent<MeshFilter>();
        ModelFilter.sharedMesh = mesh;

        NormalizeModel(model.transform);
        ApplySkin(model, skinIndex);

        // 3) 충돌 크기 = 모델 크기 (날개폭 x 높이 x 길이)
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Bounds b = GetLocalBounds(model.transform, visual);
            box.center = b.center;
            box.size = b.size;
        }

        // 4) 트레일
        AttachTrailById(trailId, planeLength, null);

        Debug.Log($"[PlaneAppearance] 스킨 {skinIndex} ({PlaneSkinState.SkinName(skinIndex)}) / 트레일 {trailIndex} → {trailId} 적용");
    }

    /// <summary>텍스처 모델 스킨 (모델 + 색 텍스처 + 고른 트레일). 모델을 못 불러오면 false</summary>
    private bool BuildModelSkin(PlaneSkins.Skin skin, int trailId)
    {
        MeshRenderer cubeRenderer = GetComponent<MeshRenderer>();

        GameObject visualObject = new GameObject("Visual");
        Transform v = visualObject.transform;
        v.SetParent(transform, false);

        MeshFilter filter;
        GameObject model = PlaneModels.CreateTextured(skin, v, skin.size, out filter);
        if (model == null)
        {
            Destroy(visualObject);
            return false;
        }

        if (cubeRenderer != null)
            cubeRenderer.enabled = false;

        transform.localScale = Vector3.one;
        visual = v;
        model.name = skin.key;
        ModelFilter = filter;

        // 충돌 크기 = 모델 크기
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Bounds b = GetLocalBounds(model.transform, visual);
            box.center = b.center;
            box.size = b.size;
        }

        // 트레일 (트레일 프리팹은 길이 1 기준 → 모델 길이에 맞춤, 나오는 자리는 이 모델의 꼬리)
        AttachTrailById(trailId, skin.size, new Vector3(skin.trailX, skin.trailY, TrailZ));
        return true;
    }

    /// <summary>스텔스 폭격기 모델 + 전용 비행운. 모델을 못 불러오면 false (종이비행기로 대신 표시)</summary>
    private bool BuildBomber(int trailId)
    {
        MeshRenderer cubeRenderer = GetComponent<MeshRenderer>();

        GameObject visualObject = new GameObject("Visual");
        Transform v = visualObject.transform;
        v.SetParent(transform, false);

        MeshFilter filter;
        GameObject model = PlaneModels.Create(PlaneModels.BomberMeshPath, v, bomberSpan, out filter);
        if (model == null)
        {
            Destroy(visualObject);
            return false;
        }

        if (cubeRenderer != null)
            cubeRenderer.enabled = false;

        transform.localScale = Vector3.one;
        visual = v;
        model.name = "StealthBomber";
        ModelFilter = filter;

        // 충돌 크기 = 모델 크기
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Bounds b = GetLocalBounds(model.transform, visual);
            box.center = b.center;
            box.size = b.size;
        }

        // 폭격기 비행운(스킨별 기본 트레일 포함)이면 전용 비행운, 다른 트레일을 골랐으면 그 트레일
        if (trailId == TrailCatalog.BomberId)
            lineTrails = BomberEffects.AttachContrails(model.transform);
        else
            AttachTrailById(trailId, bomberSpan * 0.6f, BomberTrailAnchor);

        return true;
    }

    /// <summary>비행운을 비행기에서 떼어내기 (폭발할 때 남은 꼬리가 자연스럽게 사라지게)</summary>
    public void ReleaseLineTrails()
    {
        BomberEffects.ReleaseContrails(lineTrails);
        lineTrails = null;
    }

    private static Mesh cachedMesh;

    /// <summary>줄인 종이비행기 메시 불러오기 (한 번만 만들고 재사용)</summary>
    private static Mesh LoadMesh()
    {
        if (cachedMesh != null)
            return cachedMesh;

        TextAsset data = Resources.Load<TextAsset>(MeshPath);
        if (data == null)
            return null;

        byte[] bytes = data.bytes;
        if (bytes.Length < 12 || bytes[0] != 'P' || bytes[1] != 'P' || bytes[2] != 'M' || bytes[3] != '1')
        {
            Debug.LogError("[PlaneAppearance] 모델 데이터 형식이 올바르지 않습니다.");
            return null;
        }

        int vertexCount = System.BitConverter.ToInt32(bytes, 4);
        int indexCount = System.BitConverter.ToInt32(bytes, 8);
        int offset = 12;

        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        int[] indices = new int[indexCount];

        for (int i = 0; i < vertexCount; i++, offset += 12)
            vertices[i] = new Vector3(
                System.BitConverter.ToSingle(bytes, offset),
                System.BitConverter.ToSingle(bytes, offset + 4),
                System.BitConverter.ToSingle(bytes, offset + 8));

        for (int i = 0; i < vertexCount; i++, offset += 12)
            normals[i] = new Vector3(
                System.BitConverter.ToSingle(bytes, offset),
                System.BitConverter.ToSingle(bytes, offset + 4),
                System.BitConverter.ToSingle(bytes, offset + 8));

        for (int i = 0; i < indexCount; i++, offset += 4)
            indices[i] = System.BitConverter.ToInt32(bytes, offset);

        Mesh mesh = new Mesh();
        mesh.name = "PaperPlane";
        if (vertexCount > 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = indices;
        mesh.RecalculateBounds();

        cachedMesh = mesh;
        return mesh;
    }

    /// <summary>모델 길이를 planeLength로 맞추고 가운데 정렬 (원본 크기 / 가져오기 설정과 무관하게)</summary>
    private void NormalizeModel(Transform model)
    {
        model.localPosition = Vector3.zero;
        model.localRotation = Quaternion.identity;
        model.localScale = Vector3.one;

        Bounds b = GetLocalBounds(model, visual);
        float length = Mathf.Max(b.size.z, 0.0001f);   // 모델은 기수가 +Z 방향

        float scale = planeLength / length;
        model.localScale = Vector3.one * scale;
        model.localPosition = -b.center * scale;
    }

    private static void ApplySkin(GameObject model, int skinIndex)
    {
        Material material = Resources.Load<Material>(MaterialPath);
        Color color = PlaneSkinState.SkinColor(skinIndex);

        foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
        {
            if (material != null)
            {
                Material[] mats = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = material;
                r.sharedMaterials = mats;
            }

            // 스킨씬의 PlaneColorSetter와 같은 방식 (머티리얼 복제 없이 색만 덮어쓰기)
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            r.SetPropertyBlock(block);

            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    // 트레일이 나오는 앞뒤 위치 (모델 길이 1 기준, 꼬리 끝)
    private const float TrailZ = -0.5f;

    // 종이비행기에서 트레일이 나오는 자리 (트레일 프리팹의 날개 끝 위치와 같음)
    private static readonly Vector3 PaperTrailAnchor = new Vector3(0.2f, 0.14f, -0.51f);

    // 스텔스 폭격기에 다른 트레일을 붙일 때 나오는 자리 (엔진 배기구 근처, 날개폭의 0.6배를 길이 1로 봄)
    private static readonly Vector3 BomberTrailAnchor = new Vector3(0.16f, 0.03f, -0.34f);

    /// <summary>
    /// 트레일 번호(TrailCatalog)에 맞는 트레일 붙이기.
    /// 색 트레일(프리팹)은 기존 방식, 스킨별 트레일은 TrailEffects에서 코드로 만든다.
    /// </summary>
    private void AttachTrailById(int trailId, float length, Vector3? anchor)
    {
        TrailCatalog.Trail trail = TrailCatalog.Get(trailId);

        if (trail == null || trail.prefabIndex >= 0)
        {
            AttachTrail(trail != null ? trail.prefabIndex : 0, length, anchor);
            return;
        }

        trails = TrailEffects.Attach(visual, trailId, length, anchor ?? PaperTrailAnchor);
    }

    /// <summary>
    /// 트레일 붙이기. anchor를 주면 좌우 두 줄기가 나오는 자리를 그 위치로 옮긴다.
    /// (트레일 프리팹은 종이비행기 날개 끝에 맞춰져 있어서 다른 모델에는 어긋남. x = 가운데에서 좌우 거리)
    /// </summary>
    private void AttachTrail(int trailIndex, float length, Vector3? anchor = null)
    {
        GameObject trailPrefab = Resources.Load<GameObject>(TrailPath);
        if (trailPrefab == null)
        {
            Debug.LogWarning($"[PlaneAppearance] Resources/{TrailPath} 트레일 프리팹을 찾지 못했습니다.");
            return;
        }

        GameObject trailRoot = Instantiate(trailPrefab, visual, false);
        trailRoot.name = "Trail";

        // 트레일 프리팹은 길이 1 기준으로 만들어져 있음 → 비행기 길이에 맞춤
        trailRoot.transform.localPosition = Vector3.zero;
        trailRoot.transform.localRotation = Quaternion.identity;
        trailRoot.transform.localScale = Vector3.one * length;

        int count = trailRoot.transform.childCount;
        if (count == 0)
            return;

        int selected = ((trailIndex % count) + count) % count;

        for (int i = count - 1; i >= 0; i--)
        {
            Transform child = trailRoot.transform.GetChild(i);

            if (i == selected)
            {
                child.gameObject.SetActive(true);

                if (anchor.HasValue)
                    MoveTrailEmitters(child, anchor.Value);
            }
            else
            {
                Destroy(child.gameObject);
            }
        }

        trails = trailRoot.GetComponentsInChildren<ParticleSystem>(true);
    }

    /// <summary>트레일 줄기(왼쪽 / 오른쪽)가 나오는 자리 옮기기. 원래 왼쪽에 있던 줄기는 왼쪽에 둔다.</summary>
    private static void MoveTrailEmitters(Transform trail, Vector3 anchor)
    {
        foreach (Transform emitter in trail)
        {
            float side = emitter.localPosition.x < 0f ? -1f : 1f;
            emitter.localPosition = new Vector3(side * Mathf.Abs(anchor.x), anchor.y, anchor.z);
        }
    }

    private static Bounds GetLocalBounds(Transform target, Transform space)
    {
        bool has = false;
        Bounds result = new Bounds(Vector3.zero, Vector3.zero);

        foreach (MeshFilter mf in target.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null)
                continue;

            Bounds mb = mf.sharedMesh.bounds;
            Vector3 c = mb.center;
            Vector3 e = mb.extents;

            // 메시 바운즈의 8개 꼭짓점을 space 기준 좌표로 변환
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = c + new Vector3(
                    (i & 1) == 0 ? -e.x : e.x,
                    (i & 2) == 0 ? -e.y : e.y,
                    (i & 4) == 0 ? -e.z : e.z);

                Vector3 p = space.InverseTransformPoint(mf.transform.TransformPoint(corner));

                if (!has)
                {
                    result = new Bounds(p, Vector3.zero);
                    has = true;
                }
                else
                {
                    result.Encapsulate(p);
                }
            }
        }

        return result;
    }

    // ---------- 비행 중 기울기 / 트레일 ----------

    private void Update()
    {
        if (visual == null)
            return;

        bool crashed = rb != null && rb.isKinematic;

        if (crashed)
        {
            StopTrails();
            return;
        }

        float targetBank = 0f;
        if (FlightInputManager.Instance != null)
            targetBank = Mathf.Clamp(-FlightInputManager.Instance.TurnInput * maxBank, -maxBank, maxBank);

        float targetPitch = 0f;
        if (rb != null)
        {
            Vector3 v = rb.linearVelocity;
            float horizontal = new Vector2(v.x, v.z).magnitude;
            if (horizontal > 0.05f || Mathf.Abs(v.y) > 0.05f)
                targetPitch = Mathf.Clamp(-Mathf.Atan2(v.y, Mathf.Max(horizontal, 0.01f)) * Mathf.Rad2Deg, -maxPitch, maxPitch);
        }

        float t = 1f - Mathf.Exp(-tiltSpeed * Time.deltaTime);
        bank = Mathf.Lerp(bank, targetBank, t);
        pitch = Mathf.Lerp(pitch, targetPitch, t);

        visual.localRotation = Quaternion.Euler(pitch, 0f, bank);
    }

    private void StopTrails()
    {
        if (!trailsStopped && lineTrails != null)
        {
            foreach (TrailRenderer t in lineTrails)
            {
                if (t != null)
                    t.emitting = false;
            }
        }

        if (trailsStopped || trails == null)
        {
            trailsStopped = true;
            return;
        }

        trailsStopped = true;

        foreach (ParticleSystem ps in trails)
        {
            if (ps != null)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}

/// <summary>
/// 장착한 비행기 스킨 / 트레일 번호 (스킨씬과 게임에서 공통 사용)
///
/// - 비행기 스킨 : Firebase users.equipped_skin_id (없으면 PlayerPrefs "EquippedSkin")
/// - 트레일 : Firebase users.equipped_trail_id (없으면 PlayerPrefs "EquippedTrail")
/// - 스킨 번호 : 0 기본 종이비행기(클래식 화이트), 9 스텔스 폭격기, 10번부터 텍스처 모델 스킨(PlaneSkins)
///   (1~8번 색 스킨은 삭제됨. 예전에 1~8번을 장착했던 유저는 기본 스킨으로 표시)
///   Firebase에 번호로 저장돼 있으므로 한 번 정한 번호는 바꾸지 않는다. (새 스킨은 PlaneSkins 목록 끝에 추가)
/// - 트레일 번호 : TrailCatalog 참고 (0 = 스킨별 기본 트레일, 100 = 클래식 이펙트, 1~8 색 트레일, 9~20 스킨별 트레일)
///   트레일 보유 여부는 같은 번호의 스킨 ID를 같이 쓴다.
/// </summary>
public static class PlaneSkinState
{
    public const string PlaneKey = "EquippedSkin";
    public const string TrailKey = "EquippedTrail";

    public const int DefaultSkin = 0;   // 기본 종이비행기 (클래식 화이트)
    public const int BomberSkin = 9;    // 스텔스 폭격기

    // 번호 범위 (0 ~ 가장 큰 스킨 번호). PlaneSkins에 스킨을 추가하면 자동으로 늘어난다.
    private static readonly int IdRange = Mathf.Max(BomberSkin, PlaneSkins.MaxId) + 1;

    private static readonly Color DefaultColor = new Color(1f, 1f, 1f);
    private static readonly Color BomberColor = new Color(0.37f, 0.40f, 0.44f);   // 모델 자체 색 사용, 참고용

    public static int SkinCount => IdRange;

    /// <summary>지금 쓸 수 있는 스킨 번호인지 (삭제된 1~8번은 false)</summary>
    public static bool IsAvailable(int index)
    {
        return index == DefaultSkin || index == BomberSkin || PlaneSkins.Get(index) != null;
    }

    /// <summary>삭제된 스킨 번호는 기본 스킨으로 바꾼다</summary>
    public static int NormalizeSkin(int value)
    {
        int index = Wrap(value);
        return IsAvailable(index) ? index : DefaultSkin;
    }

    public static int EquippedPlaneIndex
    {
        get
        {
            string id = null;
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentUser != null)
                id = SaveManager.Instance.CurrentUser.equipped_skin_id;

            int value = ParseId(id, PlayerPrefs.GetInt(PlaneKey, 0));
            return NormalizeSkin(value);
        }
    }

    public static int EquippedTrailIndex
    {
        get
        {
            string id = null;
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentUser != null)
                id = SaveManager.Instance.CurrentUser.equipped_trail_id;

            int value = ParseId(id, PlayerPrefs.GetInt(TrailKey, 0));
            return TrailCatalog.Normalize(value);
        }
    }

    public static Color SkinColor(int index) => NormalizeSkin(index) == BomberSkin ? BomberColor : DefaultColor;

    public static string SkinName(int index)
    {
        int id = NormalizeSkin(index);

        PlaneSkins.Skin skin = PlaneSkins.Get(id);
        if (skin != null)
            return skin.title;

        return id == BomberSkin ? "스텔스 폭격기" : "클래식 화이트";
    }

    /// <summary>"default" = 0, 숫자 문자열 = 그 번호, 없으면 fallback</summary>
    public static int ParseId(string id, int fallback)
    {
        if (string.IsNullOrEmpty(id))
            return fallback;

        if (id == "default")
            return 0;

        return int.TryParse(id, out int value) ? value : fallback;
    }

    private static int Wrap(int value)
    {
        return ((value % IdRange) + IdRange) % IdRange;
    }
}
