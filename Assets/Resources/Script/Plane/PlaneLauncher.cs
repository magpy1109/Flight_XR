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
        Transform spawn =
            SpawnPointResolver.Instance.GetSpawnTransform();

        // 수평 방향만 사용 (손/머리가 기울어져 있어도 비행기는 수평으로 출발)
        Vector3 flatForward = spawn.forward;
        flatForward.y = 0f;
        flatForward = flatForward.sqrMagnitude < 0.001f ? Vector3.forward : flatForward.normalized;
        Quaternion spawnRotation = Quaternion.LookRotation(flatForward, Vector3.up);

        // 손(또는 카메라) 앞 35cm에서 생성
        Vector3 spawnPosition =
            spawn.position + flatForward * 0.35f;

        // 비행기 생성
        currentPlane = Instantiate(
            planePrefab,
            spawnPosition,
            spawnRotation);

        // 종이비행기 모델 + 장착한 스킨 / 트레일 적용
        PlaneAppearance.Setup(currentPlane);

        // 실시간 공간 인식 충돌 (야외처럼 공간 설정 데이터가 없어도 실제 물체 / 바닥에 부딪히면 추락)
        if (currentPlane.GetComponent<PlaneEnvironmentCollision>() == null)
            currentPlane.AddComponent<PlaneEnvironmentCollision>();

        // 생성된 비행기의 컨트롤러
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

        // 게임 시작
        GameManager.Instance.StartGame();

        // 초기 속도
        Rigidbody rb =
            currentPlane.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // 발사 속도가 비행 속도보다 빠르면 비행 속도로 (출발 직후 갑자기 빨리 날아가지 않게)
            PlanePhysics physics = currentPlane.GetComponent<PlanePhysics>();
            float speed = physics != null ? Mathf.Min(launchSpeed, physics.cruiseSpeed) : launchSpeed;

            rb.linearVelocity =
                flatForward * speed;
        }
    }

    // ⭐ 재시작할 때 기존 비행기 제거
    public void ResetPlane()
    {
        if (currentPlane != null)
        {
            Destroy(currentPlane);
            currentPlane = null;
        }
    }

    public void PlaneFinished(GameObject plane)
    {
        if (currentPlane == plane)
        {
            currentPlane = null;
        }
    }
}