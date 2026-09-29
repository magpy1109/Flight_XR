using UnityEngine;

/// <summary>
/// 비행기 추락 판정.
/// - 물리 충돌 (방 공간 인식 메시 등 콜라이더에 부딪힘)
/// - 실시간 공간 인식 충돌 (PlaneEnvironmentCollision에서 Crash 호출)
/// 한 번만 게임오버 처리한다.
/// </summary>
public class PlaneCrash : MonoBehaviour
{
    private bool crashed;

    public bool IsCrashed => crashed;

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsCrashObject(collision.gameObject))
            return;

        Crash(collision.gameObject.name);
    }

    /// <summary>추락 처리 (이미 추락했으면 무시)</summary>
    public void Crash(string reason)
    {
        if (crashed)
            return;

        crashed = true;

        Debug.Log($"비행기 충돌 : {reason}");

        if (GameManager.Instance != null)
            GameManager.Instance.EndGame(gameObject);
    }

    private bool IsCrashObject(GameObject obj)
    {
        // MRUK 연결 시 여기만 수정하면 된다.
        return true;
    }
}
