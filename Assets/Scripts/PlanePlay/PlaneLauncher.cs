using UnityEngine;

public class PlaneLauncher : MonoBehaviour
{
    public GameObject planePrefab;
    public Transform spawnPoint;

    public float throwForce = 15f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            LaunchPlane();
        }
    }

    void LaunchPlane()
    {
        // 1. 플레이어 몸 밖에서 안전하게 스폰
        Vector3 spawnPosition = spawnPoint.position + spawnPoint.forward * 0.5f;

        GameObject plane = Instantiate(
            planePrefab,
            spawnPosition,
            spawnPoint.rotation
        );

        // 2. 충돌 무시 (이건 그대로 유지)
        Collider planeCollider = plane.GetComponent<Collider>();
        Collider playerCollider = GetComponent<Collider>(); 

        if (planeCollider != null && playerCollider != null)
        {
            Physics.IgnoreCollision(planeCollider, playerCollider);
        }

        Rigidbody rb = plane.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.angularVelocity = Vector3.zero;
            rb.linearDamping = 0.2f; 

            // ⚠️ [가장 중요한 수정 부분] 
            // AddForce를 지우고, 방향 * 던질 속도로 직접 속도를 덮어씌웁니다.
            // 이러면 무게(Mass)나 물리 버그를 무시하고 무조건 설정한 속도로 날아갑니다!
            rb.linearVelocity = spawnPoint.forward * throwForce; 
        }
    }
}