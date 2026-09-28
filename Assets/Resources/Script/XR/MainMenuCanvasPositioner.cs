using System.Collections;
using UnityEngine;

public class MainMenuCanvasPositioner : MonoBehaviour
{
    [Header("Position")]
    [SerializeField] private Transform centerEye;
    [SerializeField] private float distance = 1.5f;

    private IEnumerator Start()
    {
        // XR 카메라 위치가 안정된 다음 실행
        yield return null;
        yield return null;

        PositionCanvas();
    }

    private void PositionCanvas()
    {
        if (centerEye == null)
        {
            Camera mainCamera = Camera.main;

            if (mainCamera == null)
            {
                Debug.LogError("MainMenuCanvasPositioner : Main Camera를 찾을 수 없습니다.");
                return;
            }

            centerEye = mainCamera.transform;
        }

        // 눈의 위치를 기준으로 정면 방향 계산
        Vector3 forward = centerEye.forward;

        // 위아래로 고개를 숙이고 있어도 메뉴는 눈높이에 오도록
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        forward.Normalize();

        // 눈앞 1.5m에 배치
        transform.position =
            centerEye.position + forward * distance;

        // 메뉴가 사용자를 바라보도록 회전
        Vector3 lookDirection =
            centerEye.position - transform.position;

        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation =
                Quaternion.LookRotation(lookDirection, Vector3.up)
                * Quaternion.Euler(0f, 180f, 0f);
        }

        Debug.Log(
            $"MainMenuCanvas 위치 설정 : {transform.position}"
        );
    }
}