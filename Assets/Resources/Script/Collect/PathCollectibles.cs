using System.Collections.Generic;
using Meta.XR;
using Meta.XR.MRUtilityKit;
using TMPro;
using UnityEngine;

/// <summary>
/// 공중의 노란 네모(점수 아이템).
///
/// [경로]
/// - 비행이 시작되면 비행기 앞쪽으로 노란 네모 6개가 이어진 "길"이 생긴다. (0.5m 간격)
/// - 길은 완만하게 휘거나 오르내리며 랜덤하게 만들어지고, 하나를 먹으면 길 끝에 새 네모가 하나 더 이어진다.
/// - 휘는 정도 / 오르내리는 정도는 비행기가 실제로 따라갈 수 있는 범위로 제한
///   (속도 0.8m/s, 최대 회전 60°/s, 상승 0.7m/s · 하강 0.5m/s 기준)
/// - 길을 놓쳐서 네모가 모두 지나가면, 그때 비행기 앞쪽에서 새 길이 시작된다.
///
/// [먹을 수 없는 곳에는 만들지 않음]
/// - 바닥 / 책상 등 아래 물체에서 30cm 이상, 천장에서 25cm 이상 떨어진 곳
/// - 눈높이보다 40cm 넘게 높은 곳, 얼굴 바로 앞(40cm 이내)은 제외
/// - 방 공간 인식 데이터가 있으면(실내) 방 안쪽에만
/// - 벽 / 가구(방 공간 메시) 안이나 그 너머, 실제 물체(실시간 깊이) 안이나 뒤쪽 제외
/// - 앞 네모에서 다음 네모까지 날아가는 길이 막혀 있으면 제외 → 좁은 곳에서는 벽을 피해 휘어지고,
///   더 갈 곳이 없으면 길이 거기서 끝난다 (여기저기 흩뿌리지 않음)
///
/// [점수]
/// - 놓치지 않고 연속으로 먹을수록 10 → 20 → 30 → 40 → 50점 (최대 50점)
/// - 하나라도 놓치면 다시 10점부터
///
/// GameManager가 자동으로 붙인다. (씬 수정 없음)
/// </summary>
public class PathCollectibles : MonoBehaviour
{
    // ---------- 경로 ----------
    private const int VisibleCount = 6;          // 앞에 보이는 네모 개수
    private const float Spacing = 0.5f;          // 네모 간격 (m)
    private const float FirstDistance = 0.75f;   // 첫 네모까지 거리 (m, 비행기 기준)
    private const float MaxTurnPerStep = 16f;    // 평소 한 칸에 휘는 최대 각도
    private const float MaxDodgeTurn = 34f;      // 벽을 피할 때 한 칸에 휘는 최대 각도 (회전 반경 약 0.85m)
    private const float MaxRise = 0.12f;         // 한 칸에 올라가는 최대 높이 (m)
    private const float MaxDrop = 0.1f;          // 한 칸에 내려가는 최대 높이 (m)
    private const float RestartDelay = 0.6f;

    // ---------- 공간 ----------
    private const float FloorClearance = 0.3f;
    private const float CeilingClearance = 0.25f;
    private const float MaxAboveEye = 0.4f;
    private const float HeadClearance = 0.4f;
    private const float SpaceRadius = 0.12f;     // 네모 주변에 비어 있어야 하는 반경

    // ---------- 먹기 / 점수 ----------
    private const float CollectRadius = 0.13f;
    private const int BasePoints = 10;
    private const int MaxPoints = 50;

    private const string CollectSoundPath = "Sound/Collect/Collect";

    public static PathCollectibles Instance { get; private set; }

    /// <summary>현재 연속으로 먹은 개수 (놓치면 0)</summary>
    public int Combo { get; private set; }

    /// <summary>이번 판에 먹은 개수</summary>
    public int Collected { get; private set; }

    /// <summary>이번 판 최대 연속 개수</summary>
    public int MaxCombo { get; private set; }

    private readonly List<CollectibleCube> cubes = new List<CollectibleCube>();

    private Transform plane;
    private bool wasPlaying;

    // 길의 끝 (다음 네모를 이어 붙일 위치 / 방향)
    private bool hasHead;
    private bool headBlocked;
    private Vector3 headPosition;
    private float headYaw;
    private float turnRate;
    private float climbRate;
    private float restartTime;

    private EnvironmentRaycastManager environment;
    private Transform rig;
    private AudioClip collectClip;

    // ---------- 설치 ----------

    public static void Install(GameObject host)
    {
        if (host != null && host.GetComponent<PathCollectibles>() == null)
            host.AddComponent<PathCollectibles>();
    }

    private void Awake()
    {
        Instance = this;
        collectClip = Resources.Load<AudioClip>(CollectSoundPath);
        CollectibleCube.Preload();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ---------- 갱신 ----------

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        bool playing = gm != null && gm.IsPlaying;

        if (playing && !wasPlaying)
            BeginRound();
        else if (!playing && wasPlaying)
            EndRound();

        wasPlaying = playing;

        if (!playing)
            return;

        if (plane == null)
        {
            PlaneController controller = FindFirstObjectByType<PlaneController>();
            plane = controller != null ? controller.transform : null;
            if (plane == null)
                return;
        }

        CheckCubes();

        if (Time.time < restartTime)
            return;

        // 네모가 다 지나갔으면 비행기 앞에서 새 길 시작
        if (cubes.Count == 0 && hasHead)
        {
            hasHead = false;
            restartTime = Time.time + RestartDelay;
            return;
        }

        // 길 이어 붙이기 (프레임당 하나씩)
        if (cubes.Count < VisibleCount && !(hasHead && headBlocked))
        {
            if (!hasHead)
                StartPath();
            else
                ExtendPath();
        }
    }

    private void BeginRound()
    {
        ClearAll(false);
        Combo = 0;
        Collected = 0;
        MaxCombo = 0;
        plane = null;
        hasHead = false;
        headBlocked = false;
        restartTime = Time.time + 0.3f;

        if (environment == null)
            environment = FindFirstObjectByType<EnvironmentRaycastManager>();
    }

    private void EndRound()
    {
        ClearAll(true);
        hasHead = false;
        plane = null;
    }

    private void ClearAll(bool animate)
    {
        foreach (CollectibleCube cube in cubes)
        {
            if (cube == null)
                continue;

            if (animate)
                cube.Vanish();
            else
                Destroy(cube.gameObject);
        }

        cubes.Clear();
    }

    // ---------- 먹기 / 놓침 ----------

    private void CheckCubes()
    {
        Vector3 p = plane.position;
        Vector3 forward = plane.forward;

        Vector3 flat = forward;
        flat.y = 0f;
        flat = flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.forward;

        Vector3 nose = p + forward * 0.09f;
        Vector3 tail = p - forward * 0.09f;

        for (int i = cubes.Count - 1; i >= 0; i--)
        {
            CollectibleCube cube = cubes[i];
            if (cube == null)
            {
                cubes.RemoveAt(i);
                continue;
            }

            Vector3 c = cube.Center;

            if (DistanceToSegment(c, tail, nose) <= CollectRadius)
            {
                cubes.RemoveAt(i);
                Collect(cube);
                continue;
            }

            // 지나쳤거나 너무 멀어짐 → 놓침
            Vector3 rel = c - p;
            bool behind = Vector3.Dot(rel, flat) < -0.3f && rel.magnitude > 0.35f;
            bool tooFar = rel.magnitude > 3.5f;

            if (behind || tooFar)
            {
                cubes.RemoveAt(i);
                cube.Vanish();
                Combo = 0;
            }
        }
    }

    private void Collect(CollectibleCube cube)
    {
        Combo++;
        Collected++;
        MaxCombo = Mathf.Max(MaxCombo, Combo);

        int points = Mathf.Min(BasePoints * Combo, MaxPoints);

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(points);

        Vector3 position = cube.Center;
        cube.Pop();

        ScorePopup.Show(position, points, Combo);

        // 연속으로 먹을수록 소리가 조금씩 높아짐
        float pitch = 1f + 0.06f * (Mathf.Min(Combo, 6) - 1);
        PlaySound(collectClip, position, pitch);

        ControllerHaptics.Tick();
    }

    // ---------- 길 만들기 ----------

    private void StartPath()
    {
        if (plane == null)
            return;

        // 실시간 공간 인식은 비행기가 생길 때 만들어지므로 여기서 다시 찾음
        if (environment == null)
            environment = FindFirstObjectByType<EnvironmentRaycastManager>();

        Vector3 forward = plane.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        Vector3 origin = plane.position;

        // 정면 → 조금씩 옆으로 (벽을 보고 있으면 옆쪽으로 시작)
        float[] offsets = { 0f, 15f, -15f, 30f, -30f, 45f, -45f };
        float[] heights = { 0f, 0.1f, -0.1f, 0.2f };

        foreach (float offset in offsets)
        {
            foreach (float dy in heights)
            {
                float y = yaw + offset;
                Vector3 p = origin + Direction(y) * FirstDistance + Vector3.up * dy;

                if (!IsValid(origin, p))
                    continue;

                hasHead = true;
                headBlocked = false;
                headPosition = p;
                headYaw = y;
                turnRate = 0f;
                climbRate = 0f;

                Spawn(p, 0);
                return;
            }
        }

        // 지금은 갈 곳이 없음 → 잠시 뒤 다시
        restartTime = Time.time + 0.5f;
    }

    private void ExtendPath()
    {
        // 휘는 정도 / 오르내림을 조금씩 바꿔서 자연스러운 곡선
        turnRate = Mathf.Clamp(turnRate + Random.Range(-7f, 7f), -MaxTurnPerStep, MaxTurnPerStep);
        if (Random.value < 0.15f)
            turnRate *= 0.3f;   // 가끔 곧게

        climbRate = Mathf.Clamp(climbRate + Random.Range(-0.04f, 0.04f), -MaxDrop * 0.8f, MaxRise * 0.8f);

        // 너무 낮거나 높아지지 않게
        float eye = EyeY();
        float floor = FloorY(headPosition);
        if (headPosition.y < floor + 0.6f)
            climbRate = Mathf.Max(climbRate, 0.04f);
        if (headPosition.y > eye - 0.15f)
            climbRate = Mathf.Min(climbRate, -0.03f);

        float[] turnOffsets = { 0f, 8f, -8f, 16f, -16f, 26f, -26f, 40f, -40f };
        float[] climbOffsets = { 0f, 0.06f, -0.06f, 0.12f };

        foreach (float t in turnOffsets)
        {
            float turn = Mathf.Clamp(turnRate + t, -MaxDodgeTurn, MaxDodgeTurn);

            foreach (float c in climbOffsets)
            {
                float dy = Mathf.Clamp(climbRate + c, -MaxDrop, MaxRise);
                float yaw = headYaw + turn;
                Vector3 p = headPosition + Direction(yaw) * Spacing + Vector3.up * dy;

                if (!IsValid(headPosition, p))
                    continue;

                headPosition = p;
                headYaw = yaw;
                turnRate = turn;
                climbRate = dy;

                Spawn(p, cubes.Count);
                return;
            }
        }

        // 더 이어 갈 곳이 없음 → 길은 여기서 끝 (남은 네모를 다 지나가면 새 길 시작)
        headBlocked = true;
    }

    private void Spawn(Vector3 position, int order)
    {
        CollectibleCube cube = CollectibleCube.Create(position, transform, order * 0.06f);
        cubes.Add(cube);
    }

    private static Vector3 Direction(float yaw)
    {
        float r = yaw * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(r), 0f, Mathf.Cos(r));
    }

    // ---------- 공간 검사 ----------

    /// <summary>from에서 날아와 p에서 먹을 수 있는지</summary>
    private bool IsValid(Vector3 from, Vector3 p)
    {
        // 높이
        if (p.y < FloorY(p) + FloorClearance)
            return false;

        if (p.y > CeilingY(p) - CeilingClearance)
            return false;

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 eye = cam.transform.position;

            if (p.y > eye.y + MaxAboveEye)
                return false;

            if ((p - eye).sqrMagnitude < HeadClearance * HeadClearance)
                return false;

            // 실제 물체 뒤 / 안쪽 (눈에서 보이지 않는 곳)
            if (EnvironmentBlocked(eye, p, 0.08f))
                return false;
        }

        // 방 안쪽 (실내 + 방 데이터가 있을 때만)
        if (!OutdoorMode.Enabled)
        {
            MRUKRoom room = CurrentRoom();
            if (room != null && !room.IsPositionInRoom(p, true))
                return false;
        }

        // 벽 / 가구 (방 공간 메시)
        if (PhysicsBlockedAt(p))
            return false;

        if (PhysicsBlockedBetween(from, p))
            return false;

        // 날아가는 길에 실제 물체
        if (EnvironmentBlocked(from, p, -SpaceRadius))
            return false;

        return true;
    }

    private bool PhysicsBlockedAt(Vector3 p)
    {
        Collider[] hits = Physics.OverlapSphere(p, SpaceRadius, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider c in hits)
        {
            if (!IsIgnored(c))
                return true;
        }
        return false;
    }

    private bool PhysicsBlockedBetween(Vector3 a, Vector3 b)
    {
        Vector3 d = b - a;
        float length = d.magnitude;
        if (length < 0.001f)
            return false;

        RaycastHit[] hits = Physics.SphereCastAll(a, 0.05f, d / length, length, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit h in hits)
        {
            if (!IsIgnored(h.collider))
                return true;
        }
        return false;
    }

    /// <summary>비행기 / 손 · 컨트롤러 / UI 콜라이더는 장애물로 보지 않음</summary>
    private bool IsIgnored(Collider c)
    {
        if (c == null)
            return true;

        Transform t = c.transform;

        if (plane != null && t.IsChildOf(plane))
            return true;

        if (c.gameObject.layer == 5 || c.GetComponentInParent<Canvas>() != null)
            return true;

        Transform r = Rig();
        if (r != null && t.IsChildOf(r))
            return true;

        return false;
    }

    /// <summary>a → b 사이에 실제 물체(깊이)가 있는지. margin만큼 b 앞에서 끊어서 판단 (음수면 b 너머까지)</summary>
    private bool EnvironmentBlocked(Vector3 a, Vector3 b, float margin)
    {
        if (environment == null || !EnvironmentRaycastManager.IsSupported)
            return false;

        Vector3 d = b - a;
        float length = d.magnitude;
        if (length < 0.01f)
            return false;

        EnvironmentRaycastHit hit;
        if (!environment.Raycast(new Ray(a, d / length), out hit, length + 0.5f))
            return false;

        if (hit.status != EnvironmentRaycastHitStatus.Hit)
            return false;   // 카메라에 안 보이는 곳 등 → 알 수 없으면 막힌 것으로 보지 않음

        return Vector3.Distance(a, hit.point) < length - margin;
    }

    private float FloorY(Vector3 p)
    {
        float floor = TrackingFloorY();

        // 실시간 깊이 (책상 / 소파 등 위쪽 물체 포함)
        if (environment != null && EnvironmentRaycastManager.IsSupported)
        {
            EnvironmentRaycastHit hit;
            if (environment.Raycast(new Ray(p, Vector3.down), out hit, 3f) && hit.status == EnvironmentRaycastHitStatus.Hit)
                floor = Mathf.Max(floor, hit.point.y);
        }

        // 방 공간 메시
        RaycastHit[] hits = Physics.RaycastAll(p, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit h in hits)
        {
            if (!IsIgnored(h.collider))
                floor = Mathf.Max(floor, h.point.y);
        }

        return floor;
    }

    private float CeilingY(Vector3 p)
    {
        float ceiling = float.PositiveInfinity;

        if (environment != null && EnvironmentRaycastManager.IsSupported)
        {
            EnvironmentRaycastHit hit;
            if (environment.Raycast(new Ray(p, Vector3.up), out hit, 3f) && hit.status == EnvironmentRaycastHitStatus.Hit)
                ceiling = Mathf.Min(ceiling, hit.point.y);
        }

        RaycastHit[] hits = Physics.RaycastAll(p, Vector3.up, 3f, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit h in hits)
        {
            if (!IsIgnored(h.collider))
                ceiling = Mathf.Min(ceiling, h.point.y);
        }

        return ceiling;
    }

    private float EyeY()
    {
        Camera cam = Camera.main;
        return cam != null ? cam.transform.position.y : TrackingFloorY() + 1.6f;
    }

    private float TrackingFloorY()
    {
        Transform r = Rig();
        OVRCameraRig ovrRig = r != null ? r.GetComponent<OVRCameraRig>() : null;
        if (ovrRig != null && ovrRig.trackingSpace != null)
            return ovrRig.trackingSpace.position.y;

        return EyeY() - 1.6f;
    }

    private Transform Rig()
    {
        if (rig == null)
        {
            OVRCameraRig ovrRig = FindFirstObjectByType<OVRCameraRig>();
            rig = ovrRig != null ? ovrRig.transform : null;
        }
        return rig;
    }

    private static MRUKRoom CurrentRoom()
    {
        MRUK mruk = MRUK.Instance;
        if (mruk == null || !mruk.IsInitialized)
            return null;

        return mruk.GetCurrentRoom();
    }

    private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.000001f));
        return Vector3.Distance(p, a + ab * t);
    }

    // ---------- 소리 ----------

    private static void PlaySound(AudioClip clip, Vector3 position, float pitch)
    {
        if (clip == null)
            return;

        GameObject go = new GameObject("CollectSound");
        go.transform.position = position;

        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.clip = clip;
        source.pitch = pitch;
        source.volume = 0.8f;
        source.spatialBlend = 0.5f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 1.5f;
        source.maxDistance = 20f;
        source.dopplerLevel = 0f;

        if (SFXManager.Instance != null && SFXManager.Instance.sfxMixerGroup != null)
            source.outputAudioMixerGroup = SFXManager.Instance.sfxMixerGroup;

        source.Play();
        Destroy(go, clip.length / pitch + 0.2f);
    }
}

/// <summary>노란 네모 하나 (빙글빙글 돌며 살짝 위아래로 떠 있음)</summary>
public class CollectibleCube : MonoBehaviour
{
    private const float Size = 0.06f;
    private const string MaterialPath = "PlaneModel/PlaneSkin_Mat";

    private static readonly Color Yellow = new Color(1f, 0.82f, 0.08f);

    private static Material material;
    private static Mesh cubeMesh;

    private Transform model;
    private Vector3 basePosition;
    private float age;
    private float delay;
    private float phase;

    private enum State { Appearing, Idle, Popping, Vanishing }
    private State state = State.Appearing;
    private float stateTime;

    public Vector3 Center => basePosition;

    public static void Preload()
    {
        if (material == null)
            material = Resources.Load<Material>(MaterialPath);

        if (cubeMesh == null)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(temp);
        }
    }

    public static CollectibleCube Create(Vector3 position, Transform parent, float appearDelay)
    {
        Preload();

        GameObject root = new GameObject("YellowCube");
        root.transform.SetParent(parent, true);
        root.transform.position = position;

        GameObject model = new GameObject("Model", typeof(MeshFilter), typeof(MeshRenderer));
        model.transform.SetParent(root.transform, false);
        model.transform.localScale = Vector3.zero;
        model.GetComponent<MeshFilter>().sharedMesh = cubeMesh;

        MeshRenderer renderer = model.GetComponent<MeshRenderer>();
        if (material != null)
            renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", Yellow);
        block.SetColor("_Color", Yellow);
        renderer.SetPropertyBlock(block);

        CollectibleCube cube = root.AddComponent<CollectibleCube>();
        cube.model = model.transform;
        cube.basePosition = position;
        cube.delay = appearDelay;
        cube.phase = Random.Range(0f, Mathf.PI * 2f);
        cube.model.localRotation = Quaternion.Euler(25f, Random.Range(0f, 360f), 20f);
        return cube;
    }

    /// <summary>먹었을 때 : 톡 커졌다가 사라짐</summary>
    public void Pop()
    {
        state = State.Popping;
        stateTime = 0f;
    }

    /// <summary>놓쳤거나 게임이 끝났을 때 : 작아지며 사라짐</summary>
    public void Vanish()
    {
        if (state == State.Popping || state == State.Vanishing)
            return;

        state = State.Vanishing;
        stateTime = 0f;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        stateTime += dt;

        // 회전 + 둥실둥실
        model.Rotate(Vector3.up, 90f * dt, Space.World);
        transform.position = basePosition + Vector3.up * (Mathf.Sin(age * 2.5f + phase) * 0.012f);

        float scale;

        switch (state)
        {
            case State.Appearing:
            {
                float t = Mathf.Clamp01((age - delay) / 0.3f);
                scale = EaseOutBack(t);
                if (t >= 1f)
                    state = State.Idle;
                break;
            }

            case State.Popping:
            {
                float t = Mathf.Clamp01(stateTime / 0.2f);
                scale = t < 0.35f ? Mathf.Lerp(1f, 1.7f, t / 0.35f) : Mathf.Lerp(1.7f, 0f, (t - 0.35f) / 0.65f);
                model.Rotate(Vector3.up, 900f * dt, Space.World);
                if (t >= 1f)
                {
                    Destroy(gameObject);
                    return;
                }
                break;
            }

            case State.Vanishing:
            {
                float t = Mathf.Clamp01(stateTime / 0.3f);
                scale = 1f - t * t;
                if (t >= 1f)
                {
                    Destroy(gameObject);
                    return;
                }
                break;
            }

            default:
                scale = 1f;
                break;
        }

        model.localScale = Vector3.one * (Size * Mathf.Max(scale, 0f));
    }

    private static float EaseOutBack(float x)
    {
        const float s = 1.7f;
        x -= 1f;
        return x * x * ((s + 1f) * x + s) + 1f;
    }
}

/// <summary>먹은 자리에 "+20" 같은 점수가 떠올랐다가 사라짐</summary>
public class ScorePopup : MonoBehaviour
{
    private const float Lifetime = 0.8f;

    private TextMeshPro text;
    private Vector3 start;
    private float age;

    public static void Show(Vector3 position, int points, int combo)
    {
        GameObject go = new GameObject("ScorePopup");
        go.transform.position = position;

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.font = KoreanFont.Get(null);
        tmp.text = combo >= 2 ? $"+{points}\n<size=60%>{combo}연속!</size>" : $"+{points}";
        tmp.fontSize = 0.45f;   // 약 4cm 높이
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.86f, 0.15f);
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.rectTransform.sizeDelta = new Vector2(0.4f, 0.2f);

        try
        {
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = new Color32(120, 70, 0, 255);
        }
        catch (System.Exception)
        {
            // 테두리 적용 실패 시 글자만 표시
        }

        ScorePopup popup = go.AddComponent<ScorePopup>();
        popup.text = tmp;
        popup.start = position + Vector3.up * 0.05f;
        popup.Face();
    }

    private void Update()
    {
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / Lifetime);

        transform.position = start + Vector3.up * (0.12f * (1f - (1f - t) * (1f - t)));
        transform.localScale = Vector3.one * (t < 0.15f ? Mathf.Lerp(0.6f, 1.15f, t / 0.15f) : Mathf.Lerp(1.15f, 1f, (t - 0.15f) / 0.85f));
        text.alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;

        Face();

        if (t >= 1f)
            Destroy(gameObject);
    }

    /// <summary>항상 사용자 쪽을 바라보게</summary>
    private void Face()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 toText = transform.position - cam.transform.position;
        if (toText.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(toText, Vector3.up);
    }
}

/// <summary>HUD 점수 아이콘 : 노란 입체 네모 스프라이트를 실행 시 그려서 사용</summary>
public static class CubeIconSprite
{
    private static Sprite sprite;

    public static Sprite Get()
    {
        if (sprite != null)
            return sprite;

        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        // 정육면체를 비스듬히 본 모양 (위 / 왼쪽 / 오른쪽 면)
        Vector2 c = new Vector2(64f, 64f);
        float r = 54f;
        Vector2 top = c + new Vector2(0f, r);
        Vector2 upperRight = c + new Vector2(r * 0.866f, r * 0.5f);
        Vector2 lowerRight = c + new Vector2(r * 0.866f, -r * 0.5f);
        Vector2 bottom = c + new Vector2(0f, -r);
        Vector2 lowerLeft = c + new Vector2(-r * 0.866f, -r * 0.5f);
        Vector2 upperLeft = c + new Vector2(-r * 0.866f, r * 0.5f);

        Vector2[] topFace = { top, upperRight, c, upperLeft };
        Vector2[] leftFace = { upperLeft, c, bottom, lowerLeft };
        Vector2[] rightFace = { c, upperRight, lowerRight, bottom };

        Color topColor = new Color(1f, 0.9f, 0.36f);
        Color leftColor = new Color(0.98f, 0.76f, 0.05f);
        Color rightColor = new Color(0.9f, 0.6f, 0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color sum = Color.clear;
                int covered = 0;

                // 4x4 샘플로 가장자리를 부드럽게
                for (int sy = 0; sy < 4; sy++)
                {
                    for (int sx = 0; sx < 4; sx++)
                    {
                        Vector2 p = new Vector2(x + 0.125f + sx * 0.25f, y + 0.125f + sy * 0.25f);

                        if (Inside(p, topFace)) { sum += topColor; covered++; }
                        else if (Inside(p, leftFace)) { sum += leftColor; covered++; }
                        else if (Inside(p, rightFace)) { sum += rightColor; covered++; }
                    }
                }

                Color color = covered > 0 ? sum / covered : Color.clear;
                color.a = covered / 16f;
                tex.SetPixel(x, y, color);
            }
        }

        tex.Apply();
        sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return sprite;
    }

    private static bool Inside(Vector2 p, Vector2[] poly)
    {
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                inside = !inside;
        }
        return inside;
    }
}
