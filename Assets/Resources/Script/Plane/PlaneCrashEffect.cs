using Meta.XR;
using UnityEngine;

/// <summary>
/// 비행기 충돌 효과 (PlaneCrash에서 자동 실행).
///
/// 1. 충돌 효과음 : 비행기 종류(PlaneCategory)별 소리 (종이비행기 = 종이 구겨지는 "콰직")
/// 2. 찌그러짐 애니메이션 (종이비행기)
///    - 부딪힌 쪽(기수)부터 뒤쪽으로 퍼지듯 구겨지면서 주름진 종이 뭉치가 된다 (약 0.4초)
///    - 메시 꼭짓점을 직접 움직이므로 별도 모델 / 애니메이션 파일이 필요 없음
///    - 같은 위치의 꼭짓점은 항상 같이 움직여서(위치 기반 노이즈) 면이 찢어지지 않음
///    - 매번 무작위 시드라 구겨진 모양이 매번 조금씩 다름
/// 3. 튕김 / 낙하
///    - 부딪힌 반대 방향으로 살짝 튕긴 뒤, 데굴데굴 돌면서 바닥(실제 바닥 / 책상 등)으로 떨어져 한두 번 통통 튄다
///    - 바닥 높이 : 실시간 공간 인식(깊이) → 방 공간 메시 → 트래킹 바닥 순으로 찾음
///
/// 종이가 아닌 종류(예 : 공룡)는 구겨지지 않고 찌그러지듯 눌렸다 펴지며 튕기고 떨어진다.
/// 스텔스 폭격기는 폭발음과 함께 폭발하고 기체가 사라진다. (BomberEffects)
/// </summary>
public class PlaneCrashEffect : MonoBehaviour
{
    // ---------- 구겨짐 ----------
    private const float CrumpleDuration = 0.42f;   // 전체 구겨지는 시간 (초)
    private const float WaveDelay = 0.12f;         // 부딪힌 쪽 → 반대쪽까지 구겨짐이 퍼지는 시간
    private const float CrumpleAmount = 0.75f;     // 0 = 그대로, 1 = 완전히 동그란 뭉치
    private const float BallRadius = 0.2f;         // 종이 뭉치 반지름 (비행기 길이 대비)

    // ---------- 튕김 / 낙하 ----------
    private const float RecoilSpeed = 0.35f;       // 부딪힌 반대 방향으로 튕기는 속도 (m/s)
    private const float HopSpeed = 0.3f;           // 위로 살짝 튀는 속도 (m/s)
    private const float Gravity = 5f;              // 종이라서 실제 중력보다 약하게 (m/s²)
    private const float Bounce = 0.35f;            // 바닥에서 튀는 정도

    private static readonly Vector3[] CreaseAxes =
    {
        new Vector3(1f, 0.3f, 0.2f).normalized,
        new Vector3(0.2f, 1f, 0.4f).normalized,
        new Vector3(0.3f, 0.2f, 1f).normalized,
        new Vector3(0.7f, -0.7f, 0.3f).normalized,
    };

    private Transform visual;
    private string category;

    // 구겨짐 (원본 꼭짓점 기준)
    private Mesh mesh;
    private Vector3[] startVertices;
    private Vector3[] targetVertices;
    private float[] delays;
    private Vector3[] work;

    // 면마다 꼭짓점을 따로 가진 메시 (각진 주름 표현) → 원본 꼭짓점 번호
    private int[] map;
    private Vector3[] output;
    private bool crumpling;

    // 튕김 / 낙하
    private Vector3 velocity;
    private Vector3 spinAxis;
    private float spinSpeed;
    private float floorY;
    private float radius;
    private bool resting;

    private Vector3 baseScale = Vector3.one;
    private bool exploding;
    private float time;

    // ---------- 실행 ----------

    /// <summary>충돌 효과 재생 (효과음 + 애니메이션)</summary>
    public static void Play(GameObject plane, Vector3 impactDirection)
    {
        if (plane == null)
            return;

        PlaneAppearance appearance = plane.GetComponent<PlaneAppearance>();
        string category = appearance != null ? appearance.Category : PlaneCategory.Paper;

        CrashSound.Play(category, plane.transform.position);

        if (plane.GetComponent<PlaneCrashEffect>() != null)
            return;

        PlaneCrashEffect effect = plane.AddComponent<PlaneCrashEffect>();
        effect.Init(appearance, impactDirection, category);
    }

    private void Init(PlaneAppearance appearance, Vector3 impact, string planeCategory)
    {
        category = planeCategory;

        if (impact.sqrMagnitude < 0.0001f)
            impact = transform.forward;
        impact.Normalize();

        visual = appearance != null && appearance.Visual != null ? appearance.Visual : transform;
        baseScale = visual.localScale;

        float length = appearance != null ? appearance.planeLength : 0.18f;

        // 폭발하는 종류 (스텔스 폭격기) : 번쩍 → 불덩이 / 연기 / 파편, 기체는 바로 사라짐
        if (PlaneCategory.Explodes(category))
        {
            exploding = true;
            float size = appearance != null ? appearance.bomberSpan : 0.24f;
            BomberEffects.Explode(visual.position, impact, size);

            if (appearance != null)
                appearance.ReleaseLineTrails();
            return;
        }

        bool crumple = PlaneCategory.Crumples(category)
                       && appearance != null
                       && appearance.ModelFilter != null
                       && appearance.ModelFilter.sharedMesh != null;

        if (crumple)
        {
            SetupCrumple(appearance.ModelFilter, impact);
            radius = length * BallRadius * 0.85f;
        }
        else
        {
            radius = length * 0.3f;
        }

        // 튕김 : 부딪힌 반대 방향 + 살짝 위로
        Vector3 back = -impact;
        back.y = Mathf.Max(back.y, 0f);
        velocity = back * RecoilSpeed + Vector3.up * HopSpeed;

        // 회전 : 튕겨 나가는 방향으로 구르듯 + 약간 무작위
        Vector3 axis = Vector3.Cross(Vector3.up, back);
        if (axis.sqrMagnitude < 0.0001f)
            axis = Random.onUnitSphere;
        spinAxis = (axis.normalized + Random.insideUnitSphere * 0.6f).normalized;
        spinSpeed = Random.Range(320f, 520f);

        floorY = FindFloorY(visual.position);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;   // 일시정지 중

        time += dt;

        if (exploding)
        {
            // 폭발 순간 살짝 부풀었다가 사라짐
            if (time < 0.06f)
            {
                visual.localScale = baseScale * (1f + time * 3f);
            }
            else
            {
                visual.gameObject.SetActive(false);
                enabled = false;
            }
            return;
        }

        if (crumpling)
            UpdateCrumple();
        else if (!PlaneCategory.Crumples(category))
            UpdateSquash();

        UpdateMotion(dt);

        if (resting && !crumpling && time > CrumpleDuration)
            enabled = false;
    }

    // ---------- 종이 구겨짐 ----------

    private void SetupCrumple(MeshFilter filter, Vector3 impactWorld)
    {
        Mesh source = filter.sharedMesh;   // 모든 비행기가 함께 쓰는 원본 → 건드리지 않고 복사해서 사용
        Vector3[] vertices = source.vertices;
        int[] triangles = source.triangles;

        Vector3 impact = filter.transform.InverseTransformDirection(impactWorld).normalized;

        Bounds bounds = source.bounds;
        Vector3 center = bounds.center;
        float length = Mathf.Max(bounds.size.z, 0.0001f);   // 모델은 기수가 +Z
        float ballRadius = length * BallRadius;

        float maxDistance = 0.0001f;
        for (int i = 0; i < vertices.Length; i++)
            maxDistance = Mathf.Max(maxDistance, (vertices[i] - center).magnitude);

        Vector3 seed = new Vector3(Random.Range(0f, 100f), Random.Range(0f, 100f), Random.Range(0f, 100f));
        float frequency = 2.2f / length;

        int count = vertices.Length;
        startVertices = vertices;
        targetVertices = new Vector3[count];
        delays = new float[count];
        work = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            Vector3 p = vertices[i];
            Vector3 d = p - center;
            float m = d.magnitude;

            // 큰 접힘 방향 (위치 기반 노이즈)
            Vector3 q = p * frequency + seed;
            Vector3 n = new Vector3(
                Noise(q) - 0.5f,
                Noise(q + Vector3.one * 31.7f) - 0.5f,
                Noise(q + Vector3.one * 63.1f) - 0.5f);

            // 1) 가운데로 모여 종이 뭉치 모양
            Vector3 dir = m > 0.000001f ? d / m : Vector3.up;
            dir += n * 1.2f;
            dir = dir.sqrMagnitude > 0.000001f ? dir.normalized : Vector3.up;

            Vector3 ball = center + dir * (ballRadius * (0.45f + 0.55f * Mathf.Sqrt(m / maxDistance)));
            Vector3 target = p + (ball - p) * CrumpleAmount;

            // 2) 울퉁불퉁한 표면
            target += n * (length * 0.06f);

            // 3) 날카로운 주름선 (여러 방향의 접힘)
            for (int k = 0; k < CreaseAxes.Length; k++)
            {
                Vector3 cq = p * (frequency * 2.8f) + seed * (k + 2);
                float ridge = 0.5f - Mathf.Abs(Noise(cq) - 0.5f) * 2f;
                target += CreaseAxes[k] * (ridge * length * 0.045f);
            }

            // 4) 부딪힌 쪽은 더 납작하게 눌림
            float along = Vector3.Dot(d, impact) / (length * 0.5f);
            target -= impact * (Mathf.Clamp01(along) * length * 0.12f);

            targetVertices[i] = target;

            // 부딪힌 쪽(along = 1)부터 먼저 구겨짐
            delays[i] = (1f - Mathf.Clamp01((along + 1f) * 0.5f)) * WaveDelay;
        }

        // 면마다 꼭짓점을 따로 가진 메시로 만들어서 주름이 각지게 보이도록
        map = (int[])triangles.Clone();
        output = new Vector3[map.Length];
        int[] indices = new int[map.Length];

        for (int i = 0; i < map.Length; i++)
        {
            output[i] = vertices[map[i]];
            indices[i] = i;
        }

        mesh = new Mesh();
        mesh.name = "PaperPlane_Crumpled";
        if (output.Length > 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.MarkDynamic();
        mesh.vertices = output;
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.bounds = source.bounds;   // 구겨지면 작아지므로 원본 크기 그대로 둬도 됨

        filter.sharedMesh = mesh;
        crumpling = true;
    }

    private void UpdateCrumple()
    {
        float span = CrumpleDuration - WaveDelay;

        for (int i = 0; i < work.Length; i++)
        {
            float t = Mathf.Clamp01((time - delays[i]) / span);
            work[i] = Vector3.LerpUnclamped(startVertices[i], targetVertices[i], EaseOutBack(t));
        }

        for (int i = 0; i < output.Length; i++)
            output[i] = work[map[i]];

        mesh.vertices = output;
        mesh.RecalculateNormals();

        if (time >= CrumpleDuration)
            crumpling = false;
    }

    // 끝에서 살짝 더 오그라들었다가 돌아오는 느낌
    private static float EaseOutBack(float x)
    {
        const float s = 1.3f;
        x -= 1f;
        return x * x * ((s + 1f) * x + s) + 1f;
    }

    // ---------- 종이가 아닌 종류 : 찌그러졌다 펴짐 ----------

    private void UpdateSquash()
    {
        const float duration = 0.3f;

        if (time >= duration)
        {
            visual.localScale = baseScale;
            return;
        }

        float s = Mathf.Sin(time / duration * Mathf.PI) * (1f - time / duration);
        visual.localScale = Vector3.Scale(baseScale, new Vector3(1f + 0.35f * s, 1f - 0.45f * s, 1f + 0.35f * s));
    }

    // ---------- 튕김 / 낙하 ----------

    private void UpdateMotion(float dt)
    {
        if (resting)
            return;

        Vector3 position = visual.position;

        velocity += Vector3.down * (Gravity * dt);
        position += velocity * dt;

        float bottom = floorY + radius;
        if (position.y <= bottom)
        {
            position.y = bottom;

            if (-velocity.y > 0.25f)
            {
                // 통통 튀기
                velocity.y = -velocity.y * Bounce;
                velocity.x *= 0.6f;
                velocity.z *= 0.6f;
                spinSpeed *= 0.5f;
            }
            else
            {
                velocity = Vector3.zero;
                spinSpeed = 0f;
                resting = true;
            }
        }

        visual.position = position;

        spinSpeed = Mathf.Lerp(spinSpeed, 0f, 1f - Mathf.Exp(-2f * dt));
        visual.rotation = Quaternion.AngleAxis(spinSpeed * dt, spinAxis) * visual.rotation;
    }

    /// <summary>떨어질 바닥 높이 (실시간 공간 인식 → 방 공간 메시 → 트래킹 바닥)</summary>
    private float FindFloorY(Vector3 from)
    {
        Vector3 origin = from + Vector3.up * 0.02f;
        float best = float.NegativeInfinity;

        // 1) 실시간 공간 인식 (야외 / 공간 설정 없는 곳에서도 동작)
        if (EnvironmentRaycastManager.IsSupported)
        {
            EnvironmentRaycastManager env = FindFirstObjectByType<EnvironmentRaycastManager>();
            EnvironmentRaycastHit hit;

            if (env != null
                && env.Raycast(new Ray(origin, Vector3.down), out hit, 5f)
                && hit.status == EnvironmentRaycastHitStatus.Hit)
            {
                best = Mathf.Max(best, hit.point.y);
            }
        }

        // 2) 방 공간 메시 (자기 자신 콜라이더 제외)
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 5f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider != null && !hits[i].collider.transform.IsChildOf(transform))
                best = Mathf.Max(best, hits[i].point.y);
        }

        // 3) 트래킹 바닥
        if (float.IsNegativeInfinity(best))
        {
            OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
            if (rig != null && rig.trackingSpace != null)
                best = rig.trackingSpace.position.y;
        }

        // 바닥을 못 찾으면 제자리에서 살짝 튀고 멈춤
        if (float.IsNegativeInfinity(best) || best > from.y)
            best = from.y - radius;

        return best;
    }

    // ---------- 노이즈 ----------

    private static float Noise(Vector3 p)
    {
        int x = Mathf.FloorToInt(p.x);
        int y = Mathf.FloorToInt(p.y);
        int z = Mathf.FloorToInt(p.z);

        float fx = Smooth(p.x - x);
        float fy = Smooth(p.y - y);
        float fz = Smooth(p.z - z);

        float a = Mathf.Lerp(Hash(x, y, z), Hash(x + 1, y, z), fx);
        float b = Mathf.Lerp(Hash(x, y + 1, z), Hash(x + 1, y + 1, z), fx);
        float c = Mathf.Lerp(Hash(x, y, z + 1), Hash(x + 1, y, z + 1), fx);
        float d = Mathf.Lerp(Hash(x, y + 1, z + 1), Hash(x + 1, y + 1, z + 1), fx);

        return Mathf.Lerp(Mathf.Lerp(a, b, fy), Mathf.Lerp(c, d, fy), fz);
    }

    private static float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private static float Hash(int x, int y, int z)
    {
        unchecked
        {
            uint n = (uint)x * 374761393u + (uint)y * 668265263u + (uint)z * 1274126177u;
            n = (n ^ (n >> 13)) * 1274126177u;
            n ^= n >> 16;
            return (n & 0x7fffffffu) / 2147483647f;
        }
    }
}
