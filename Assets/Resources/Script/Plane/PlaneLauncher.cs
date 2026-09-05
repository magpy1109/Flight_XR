using UnityEngine;

public class PlaneLauncher : MonoBehaviour
{
    [SerializeField] private GameObject planePrefab;
    [SerializeField] private float launchSpeed = 5f;

    private GameObject currentPlane;

    public bool HasPlane => currentPlane != null;

    public void Launch()
    {
        Debug.Log("=== PLANE LAUNCH ===");

        if (currentPlane != null)
        {
            Debug.Log("이미 비행기 존재");
            return;
        }

        // Spawn 위치 가져오기
        Transform spawn = SpawnPointResolver.Instance.GetSpawnTransform();

        // 손(또는 카메라) 앞 35cm에서 생성
        Vector3 spawnPosition = spawn.position + spawn.forward * 0.35f;

        // ⭐ 먼저 비행기 생성
        currentPlane = Instantiate(
            planePrefab,
            spawnPosition,
            spawn.rotation);

        // ⭐ 생성된 비행기에서 컨트롤러 가져오기
        PlaneController controller =
            currentPlane.GetComponent<PlaneController>();

        if (controller != null)
        {
            controller.StartFlight();

            controller.OnPlaneDestroyed += () =>
            {
                currentPlane = null;
            };
        }

        GameManager.Instance.StartGame();

        Rigidbody rb = currentPlane.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity =
                spawn.forward * launchSpeed;
        }
    }
}