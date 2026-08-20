using UnityEngine;

public class MeshFixer : MonoBehaviour
{
    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null && mf.mesh != null)
        {
            // 거리 계산 오류를 무시하고, 화면에 무조건 렌더링 되도록 크기를 지구만큼 키워버립니다!
            mf.mesh.bounds = new Bounds(Vector3.zero, new Vector3(100000f, 100000f, 100000f));
        }
    }
}