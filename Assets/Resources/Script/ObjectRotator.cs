using UnityEngine;

public class ObjectRotator : MonoBehaviour
{
    [Header("회전 속도 (X, Y, Z축)")]
    public Vector3 rotationSpeed = new Vector3(0f, 30f, 0f);

    void Update()
    {
        // 매 프레임마다 설정한 속도만큼 오브젝트를 부드럽게 회전시킵니다.
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}