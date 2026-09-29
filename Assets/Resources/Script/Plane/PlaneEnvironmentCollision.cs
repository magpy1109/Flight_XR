using Meta.XR;
using UnityEngine;

/// <summary>
/// 실시간 공간 인식으로 비행기 충돌 판정 (공간 설정 데이터가 없는 야외에서도 동작).
///
/// Meta MRUK의 EnvironmentRaycastManager로 헤드셋 깊이 카메라가 지금 보고 있는 실제 환경에 레이를 쏜다.
/// - 앞 : 날아가는 방향으로 짧게 → 나무 / 벽 / 사람 등에 닿으면 추락
/// - 아래 : 바닥(잔디 / 땅)에 닿으면 추락
/// - 깊이 정보가 없을 때(지원 안 되는 기기, 에디터)에는 트래킹 바닥 높이(y=0)를 바닥으로 사용
/// 방 공간 인식 메시가 있으면 기존 물리 충돌(PlaneCrash)도 그대로 함께 동작한다.
///
/// 제한 : 헤드셋 카메라 시야 안에 있을 때만 판정된다. (시야 밖이면 그 순간은 판정하지 않음)
/// PlaneLauncher가 비행기를 만들 때 자동으로 붙인다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlaneEnvironmentCollision : MonoBehaviour
{
    [Tooltip("발사 직후 판정하지 않는 시간 (손 / 컨트롤러에 걸리지 않게)")]
    public float launchGrace = 0.5f;

    [Tooltip("앞쪽 여유 거리 (m) : 기수 끝에서 이만큼 안에 물체가 있으면 충돌")]
    public float frontMargin = 0.04f;

    [Tooltip("바닥 여유 거리 (m)")]
    public float groundMargin = 0.03f;

    [Tooltip("연속으로 이 횟수만큼 닿아야 충돌 (깊이 잡음으로 잘못 추락하지 않게)")]
    public int requiredHits = 2;

    private static EnvironmentRaycastManager manager;
    private static bool managerChecked;

    private Rigidbody rb;
    private PlaneCrash crash;
    private BoxCollider box;

    private float startTime;
    private int frontHits;
    private int groundHits;

    private float halfLength = 0.09f;
    private float halfHeight = 0.03f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        manager = null;
        managerChecked = false;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        crash = GetComponent<PlaneCrash>();
        box = GetComponent<BoxCollider>();
        startTime = Time.time;

        if (box != null)
        {
            halfLength = box.size.z * 0.5f;
            halfHeight = box.size.y * 0.5f;
        }

        EnsureManager();
    }

    /// <summary>현재 씬에 EnvironmentRaycastManager가 없으면 만든다.</summary>
    private static void EnsureManager()
    {
        if (manager != null)
            return;

        manager = FindFirstObjectByType<EnvironmentRaycastManager>();

        if (manager == null)
        {
            GameObject go = new GameObject("[EnvironmentRaycast]");
            manager = go.AddComponent<EnvironmentRaycastManager>();
        }

        if (!managerChecked)
        {
            managerChecked = true;
            Debug.Log($"[PlaneEnvironmentCollision] 실시간 공간 인식 지원 = {EnvironmentRaycastManager.IsSupported}");
        }
    }

    private void FixedUpdate()
    {
        if (crash == null || crash.IsCrashed || rb == null || rb.isKinematic)
            return;

        if (Time.time - startTime < launchGrace)
            return;

        Vector3 position = rb.position;

        // 트래킹 바닥(y=0) 아래로 내려가면 추락 (깊이 정보가 없는 환경의 안전장치)
        if (position.y - halfHeight <= GetTrackingFloorY())
        {
            crash.Crash("바닥 (트래킹 기준)");
            return;
        }

        if (manager == null || !EnvironmentRaycastManager.IsSupported)
            return;

        // 앞 : 날아가는 방향
        Vector3 velocity = rb.linearVelocity;
        if (velocity.sqrMagnitude > 0.0001f)
        {
            Vector3 dir = velocity.normalized;
            float lookAhead = halfLength + frontMargin + velocity.magnitude * Time.fixedDeltaTime;

            if (HitWithin(new Ray(position, dir), lookAhead))
                frontHits++;
            else
                frontHits = 0;

            if (frontHits >= requiredHits)
            {
                crash.Crash("실제 물체 (실시간 공간 인식)");
                return;
            }
        }

        // 아래 : 바닥
        if (HitWithin(new Ray(position, Vector3.down), halfHeight + groundMargin))
            groundHits++;
        else
            groundHits = 0;

        if (groundHits >= requiredHits)
            crash.Crash("바닥 (실시간 공간 인식)");
    }

    private static bool HitWithin(Ray ray, float distance)
    {
        EnvironmentRaycastHit hit;

        if (!manager.Raycast(ray, out hit, distance + 0.5f))
            return false;

        if (hit.status != EnvironmentRaycastHitStatus.Hit)
            return false;

        return Vector3.Distance(ray.origin, hit.point) <= distance;
    }

    private static Transform trackingSpace;

    private static float GetTrackingFloorY()
    {
        if (trackingSpace == null)
        {
            OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
            if (rig != null)
                trackingSpace = rig.trackingSpace;
        }

        // 트래킹 원점이 바닥 기준(Floor Level)이라 trackingSpace의 y가 실제 바닥 높이
        return trackingSpace != null ? trackingSpace.position.y : -100f;
    }
}
