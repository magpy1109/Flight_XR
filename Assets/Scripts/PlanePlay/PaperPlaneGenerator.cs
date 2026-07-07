using UnityEngine;

// ⚠️ 수정됨: MeshCollider 강제 추가 조건을 제거했습니다!
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PaperPlaneGenerator : MonoBehaviour
{
    void Awake()
    {
        Mesh mesh = new Mesh();
        mesh.name = "PaperPlaneMesh";

        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0, 0, 1),       // 0: 코 (Nose)
            new Vector3(-0.5f, 0, -1),  // 1: 왼쪽 날개 끝
            new Vector3(0.5f, 0, -1),   // 2: 오른쪽 날개 끝
            new Vector3(0, -0.3f, -1),  // 3: 꼬리 아래쪽 (손잡이)
            new Vector3(0, 0.1f, -1)    // 4: 꼬리 위쪽 (접힌 부분)
        };

        // ⚠️ 면을 겹치지 않게 원래대로 복구! (빛 계산 정상화)
        int[] triangles = new int[]
        {
            0, 1, 4, 
            0, 4, 2, 
            0, 3, 1, 
            0, 2, 3  
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals(); // 이제 빛을 완벽하게 받습니다!

        GetComponent<MeshFilter>().sharedMesh = mesh;
    }
}