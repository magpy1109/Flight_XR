using UnityEngine;

/// <summary>
/// 게임 비행기의 겉모습 (종이비행기 모델 + 장착한 스킨 색 + 장착한 트레일).
///
/// PlaneLauncher가 비행기를 만들 때 PlaneAppearance.Setup(plane)을 호출한다.
/// - 기존 네모(Cube) 표시는 끄고, 종이비행기 모델을 붙인다.
///   모델 데이터(Resources/PlaneModel/PaperPlaneMesh.bytes)는 스킨씬과 같은 원본(3D_FLIGHT.fbx, 약 195만 삼각형)을
///   Quest에서 가볍게 돌도록 약 1만2천 삼각형으로 줄인 것. (기수 +Z, 위 +Y, 길이 1)
/// - 스킨 색 : 스킨씬의 비행기 스킨과 같은 9가지 색 (장착 번호 % 9)
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
    private bool trailsStopped;

    private float bank;
    private float pitch;

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
        model.GetComponent<MeshFilter>().sharedMesh = mesh;

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
        AttachTrail(trailIndex);

        Debug.Log($"[PlaneAppearance] 스킨 {skinIndex} ({PlaneSkinState.SkinName(skinIndex)}) / 트레일 {trailIndex} 적용");
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

    private void AttachTrail(int trailIndex)
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
        trailRoot.transform.localScale = Vector3.one * planeLength;

        int count = trailRoot.transform.childCount;
        if (count == 0)
            return;

        int selected = ((trailIndex % count) + count) % count;

        for (int i = count - 1; i >= 0; i--)
        {
            Transform child = trailRoot.transform.GetChild(i);

            if (i == selected)
                child.gameObject.SetActive(true);
            else
                Destroy(child.gameObject);
        }

        trails = trailRoot.GetComponentsInChildren<ParticleSystem>(true);
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
        if (trailsStopped || trails == null)
            return;

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
/// - 스킨씬 버튼 번호는 페이지마다 이어지므로(0~44) 9로 나눈 나머지로 색을 고른다. (기존 방식과 동일)
/// </summary>
public static class PlaneSkinState
{
    public const string PlaneKey = "EquippedSkin";
    public const string TrailKey = "EquippedTrail";

    // 스킨씬 FlightPanel의 PlaneColorSetter 색상과 같은 순서
    private static readonly string[] SkinNames =
    {
        "클래식 화이트", "스카이 블루", "로즈 레드", "선샤인 옐로", "스텔스 블랙",
        "밀리터리 카모", "오로라", "크래프트", "갤럭시"
    };

    private static readonly Color[] SkinColors =
    {
        new Color(1f, 1f, 1f),                       // ClassicWhite
        new Color(0.47058824f, 0.79607844f, 0.90588236f), // SkyBlue
        new Color(1f, 0f, 0f),                       // RoseRed
        new Color(1f, 1f, 0f),                       // SunshineYellow
        new Color(0f, 0f, 0f),                       // StealthBlack
        new Color(0.3254902f, 0.3882353f, 0.28627452f),   // MillitaryCamo
        new Color(0.8745098f, 1f, 0.9764706f),       // Orora
        new Color(0.56078434f, 0.45882353f, 0.27058825f), // Craft
        new Color(0.105882354f, 0.007843138f, 0.27450982f) // Galaxy
    };

    public static int SkinCount => SkinColors.Length;

    public static int EquippedPlaneIndex
    {
        get
        {
            string id = null;
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentUser != null)
                id = SaveManager.Instance.CurrentUser.equipped_skin_id;

            int value = ParseId(id, PlayerPrefs.GetInt(PlaneKey, 0));
            return Wrap(value);
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
            return Wrap(value);
        }
    }

    public static Color SkinColor(int index) => SkinColors[Wrap(index)];

    public static string SkinName(int index) => SkinNames[Wrap(index)];

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
        int n = SkinColors.Length;
        return ((value % n) + n) % n;
    }
}
