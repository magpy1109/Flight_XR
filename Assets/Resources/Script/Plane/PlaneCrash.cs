using UnityEngine;

public class PlaneCrash : MonoBehaviour
{
    private bool crashed;

    private void OnCollisionEnter(Collision collision)
    {
        if (crashed)
            return;

        if (!IsCrashObject(collision.gameObject))
            return;

        crashed = true;

        Debug.Log($"비행기 충돌 : {collision.gameObject.name}");

        GameManager.Instance.EndGame(gameObject);
    }

    private bool IsCrashObject(GameObject obj)
    {
        // MRUK 연결 시 여기만 수정하면 된다.
        return true;
    }
}