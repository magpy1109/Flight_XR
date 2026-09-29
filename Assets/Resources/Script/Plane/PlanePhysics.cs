using UnityEngine;

/// <summary>
/// 종이비행기 비행 물리.
///
/// - 수평 속도 : 항상 앞으로 cruiseSpeed(기본 0.8m/s, 천천히 걸으며 따라갈 수 있는 속도)에 맞춰 난다. (발사 속도가 빨라도 곧 이 속도로 맞춰짐)
///   예전에는 앞으로 미는 힘이 계속 쌓여서 최대 약 30m/s까지 빨라졌음.
/// - 중력 : 실제 중력의 gravityScale 배(기본 0.12배)만 적용해서 천천히 떨어진다.
///   예전에는 실제 중력(9.8)이 입김 상승력(최대 5)보다 커서 불어도 떠오르지 않았음.
/// - 상승 / 하강 속도는 maxClimbSpeed / maxFallSpeed로 제한해서 둥실 뜨는 느낌.
///
/// 값은 비행기 프리팹의 PlanePhysics 컴포넌트에서 조절할 수 있다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlanePhysics : MonoBehaviour
{
    private Rigidbody rb;

    [Header("Speed")]
    [Tooltip("앞으로 나는 속도 (m/s)")]
    public float cruiseSpeed = 0.8f;

    [Tooltip("목표 속도로 맞춰지는 빠르기 (m/s²)")]
    public float forwardForce = 3f;

    [Header("Turn")]
    public float turnSpeed = 60f;

    [Header("Lift")]
    [Tooltip("입김 최대일 때 위로 올리는 힘 (m/s²)")]
    public float liftForce = 3f;

    [Tooltip("최대 상승 속도 (m/s)")]
    public float maxClimbSpeed = 0.7f;

    [Header("Gravity")]
    [Tooltip("실제 중력 대비 배율 (1 = 실제 중력, 0.3 = 30%)")]
    [Range(0f, 1f)]
    public float gravityScale = 0.12f;

    [Tooltip("최대 하강 속도 (m/s)")]
    public float maxFallSpeed = 0.5f;

    float currentTurn;
    float currentLift;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // 중력은 아래 FixedUpdate에서 배율을 적용해 직접 처리
        rb.useGravity = false;
    }

    public void SetTurn(float value)
    {
        currentTurn = value;
    }

    public void SetLift(float value)
    {
        currentLift = value;
    }

    void FixedUpdate()
    {
        // 추락 후(EndGame에서 kinematic으로 고정) 에는 움직이지 않음
        if (rb.isKinematic)
            return;

        float dt = Time.fixedDeltaTime;

        // 회전
        rb.MoveRotation(
            rb.rotation *
            Quaternion.Euler(
                0,
                currentTurn * turnSpeed * dt,
                0));

        Vector3 velocity = rb.linearVelocity;

        // ---- 수평 : 비행기가 향한 방향으로 cruiseSpeed ----
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.zero;

        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        horizontal = Vector3.MoveTowards(horizontal, forward * cruiseSpeed, forwardForce * dt);

        // ---- 수직 : 약한 중력 + 입김 상승 ----
        float vertical = velocity.y;
        vertical += (Physics.gravity.y * gravityScale + currentLift * liftForce) * dt;
        vertical = Mathf.Clamp(vertical, -maxFallSpeed, maxClimbSpeed);

        rb.linearVelocity = horizontal + Vector3.up * vertical;
    }
}
