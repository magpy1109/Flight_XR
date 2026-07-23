using UnityEngine;

public class PlaneLauncher : MonoBehaviour
{
    [SerializeField] private GameObject planePrefab;
    [SerializeField] private float launchSpeed = 5f;

    private GameObject currentPlane;

    public bool HasPlane => currentPlane != null;

    public void Launch()
    {
        if (currentPlane != null)
            return;

        // Spawn 위치 가져오기
        Transform spawn = SpawnPointResolver.Instance.GetSpawnTransform();

        // 손(또는 카메라) 앞 35cm에서 생성
        Vector3 spawnPosition = spawn.position + spawn.forward * 0.35f;

        currentPlane = Instantiate(
            planePrefab,
            spawnPosition,
            spawn.rotation);

        GameManager.Instance.StartGame();

        Rigidbody rb = currentPlane.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = spawn.forward * launchSpeed;
        }

        PlaneController controller =
            currentPlane.GetComponent<PlaneController>();

        if (controller != null)
        {
            controller.OnPlaneDestroyed += () =>
            {
                currentPlane = null;
            };
        }
    }
}