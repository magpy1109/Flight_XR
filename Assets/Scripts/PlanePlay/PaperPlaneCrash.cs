using UnityEngine;

public class PaperPlaneCrash : MonoBehaviour
{
    private MeshFilter meshFilter;
    private Mesh clonedMesh;
    private bool isCrashed = false;
    private PaperPlaneFlight flightScript;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        flightScript = GetComponent<PaperPlaneFlight>();
        
        if (meshFilter != null && meshFilter.sharedMesh != null)
            {
            // 이 비행기만 독립적으로 구겨지도록 메쉬를 복제합니다.
            clonedMesh = Instantiate(meshFilter.sharedMesh);
            meshFilter.sharedMesh = clonedMesh;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isCrashed) return;
        if (flightScript != null && !flightScript.IsFlying()) return;
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.name.Contains("Player")) return;

        // 천장 충돌은 이전처럼 무시 (안전하게 튕겨나감)
        foreach (ContactPoint contactCheck in collision.contacts)
        {
            if (contactCheck.normal.y < -0.5f) return; 
        }

        isCrashed = true;

        // 1. 비행기 조종 장치 종료 (플레이어 발 묶임 해제)
        if (flightScript != null) flightScript.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 🌟 [핵심 1: 빙글빙글 방지] 부딪히는 순간 물리 엔진의 '회전 기능'을 아예 잠가버립니다!
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.angularVelocity = Vector3.zero;

            // 🌟 [핵심 2: 속도 및 고도 감소] 
            // 유니티의 진짜 중력을 다시 켜서 바닥으로 자연스럽게 툭 떨어지게 만듭니다!
            rb.useGravity = true; 
            // 가던 속도를 70% 깎아버리고(0.3 곱하기), 아래쪽으로 살짝 힘을 주어 고도를 떨어뜨립니다.
            rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.3f, -1.5f, rb.linearVelocity.z * 0.3f);
        }

        // 🌟 [핵심 3: 부딪힌 방향 쪽 파츠만 정밀 구기기]
        if (clonedMesh != null && collision.contacts.Length > 0)
        {
            // 첫 번째 충돌 지점 정보를 가져옵니다.
            ContactPoint contact = collision.contacts[0];
            
            // 월드 기준의 충돌 좌표를 비행기 기준의 '로컬 좌표'로 변환합니다.
            Vector3 localContactPoint = transform.InverseTransformPoint(contact.point);
            Vector3 localNormal = transform.InverseTransformDirection(contact.normal);

            Vector3[] vertices = clonedMesh.vertices;
            
            for (int i = 0; i < vertices.Length; i++)
            {
                // 비행기 전체를 구기는 게 아니라, 실제 부딪힌 지점과 가까운(0.6 거리 이내) 정점(Vertex)들만 찾습니다.
                float distance = Vector3.Distance(vertices[i], localContactPoint);
                if (distance < 0.6f) 
                {
                    // 왼쪽 날개가 부딪히면 왼쪽이, 앞코가 부딪히면 앞코가 부딪힌 방향 안쪽으로 찌그러집니다!
                    vertices[i] += localNormal * 0.15f; 
                }
            }

            // 변경된 모양을 메쉬에 적용하고 그림자/표면을 재계산합니다.
            clonedMesh.vertices = vertices;
            clonedMesh.RecalculateNormals();
            clonedMesh.RecalculateBounds();
        }

        // 기존에 전체 크기를 강제로 찌그러트리던 코드(transform.localScale)는 파츠만 구기기 위해 삭제했습니다!
    }
}