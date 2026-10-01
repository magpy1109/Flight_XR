using UnityEngine;

/// <summary>
/// 비행기 추락 판정.
/// - 물리 충돌 (방 공간 인식 메시 등 콜라이더에 부딪힘)
/// - 실시간 공간 인식 충돌 (PlaneEnvironmentCollision에서 Crash 호출)
/// 한 번만 게임오버 처리한다.
///
/// 부딪히면 PlaneCrashEffect로 충돌 효과음(비행기 종류별) + 찌그러짐 애니메이션을 재생한다.
/// </summary>
public class PlaneCrash : MonoBehaviour
{
    private bool crashed;

    public bool IsCrashed => crashed;

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsCrashObject(collision.gameObject))
            return;

        // 부딪힌 면의 반대 방향 = 비행기가 들이받은 방향
        Vector3 impact = collision.contactCount > 0 ? -collision.GetContact(0).normal : Vector3.zero;

        Crash(collision.gameObject.name, impact);
    }

    /// <summary>추락 처리 (이미 추락했으면 무시)</summary>
    public void Crash(string reason)
    {
        Crash(reason, Vector3.zero);
    }

    /// <summary>추락 처리. impactDirection = 비행기가 부딪힌 방향 (모르면 zero → 날아가던 방향 사용)</summary>
    public void Crash(string reason, Vector3 impactDirection)
    {
        if (crashed)
            return;

        crashed = true;

        Debug.Log($"비행기 충돌 : {reason}");

        // GameManager가 비행기를 멈추기 전에 날아가던 방향을 읽어 둔다
        Vector3 impact = GetImpactDirection(impactDirection);

        // 충돌 효과음 + 찌그러짐 애니메이션
        PlaneCrashEffect.Play(gameObject, impact);

        if (GameManager.Instance != null)
            GameManager.Instance.EndGame(gameObject);
    }

    private Vector3 GetImpactDirection(Vector3 given)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        Vector3 velocity = rb != null ? rb.linearVelocity : Vector3.zero;

        if (given.sqrMagnitude > 0.0001f)
        {
            // 충돌 면 방향이 날아가던 방향과 반대로 나오면 뒤집기
            if (velocity.sqrMagnitude > 0.0001f && Vector3.Dot(given, velocity) < 0f)
                given = -given;

            return given.normalized;
        }

        if (velocity.sqrMagnitude > 0.0001f)
            return velocity.normalized;

        return transform.forward;
    }

    private bool IsCrashObject(GameObject obj)
    {
        // MRUK 연결 시 여기만 수정하면 된다.
        return true;
    }
}
