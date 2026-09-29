using System.Collections;
using UnityEngine;

/// <summary>
/// 메뉴 Canvas(메인메뉴 / 설정 / 리더보드 / 스킨)를 사용자 앞에 배치한다.
///
/// - 처음 한 번은 사용자 정면(수평) 1.5m 앞에 배치하고 그 위치를 기억한다.
/// - 이후 씬이 바뀌어도 기억한 위치를 그대로 사용한다.
///   (예전에는 씬이 바뀔 때마다 "지금 바라보는 방향"에 놓아서,
///    왼쪽 위 뒤로가기 버튼을 보며 누르면 다음 화면이 계속 왼쪽으로 밀렸음)
/// - 사용자가 손잡이로 Canvas를 옮기면(CanvasGrabHandle) 그 위치를 새로 기억한다.
/// - 기억한 위치가 너무 멀어졌거나(걸어서 이동한 경우) 뒤쪽에 있으면 다시 정면에 배치한다.
/// </summary>
public class MainMenuCanvasPositioner : MonoBehaviour
{
    [Header("Position")]
    [SerializeField] private Transform centerEye;
    [SerializeField] private float distance = 1.5f;

    [Header("Remember")]
    [Tooltip("기억한 위치가 머리에서 이 거리(m)보다 멀면 다시 정면에 배치")]
    [SerializeField] private float maxReuseDistance = 3f;

    [Tooltip("기억한 위치가 시선에서 이 각도(도)보다 벗어나 있으면 다시 정면에 배치")]
    [SerializeField] private float maxReuseAngle = 100f;

    // 씬이 바뀌어도 유지되는 메뉴 위치 (카메라 리그가 씬을 넘어가므로 월드 좌표 그대로 사용 가능)
    private static bool hasSavedPose;
    private static Vector3 savedPosition;
    private static Quaternion savedRotation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        hasSavedPose = false;
    }

    /// <summary>메뉴 위치 기억 (Canvas를 옮겼을 때 호출)</summary>
    public static void SavePose(Vector3 position, Quaternion rotation)
    {
        savedPosition = position;
        savedRotation = rotation;
        hasSavedPose = true;
    }

    /// <summary>기억한 위치를 지우고 다음 씬부터 다시 정면에 배치</summary>
    public static void ClearSavedPose()
    {
        hasSavedPose = false;
    }

    private IEnumerator Start()
    {
        // XR 카메라 위치가 안정된 다음 실행
        yield return null;
        yield return null;

        PositionCanvas();
    }

    /// <summary>지금 정면에 다시 배치 (필요하면 버튼 등에서 호출)</summary>
    public void RecenterToHead()
    {
        Transform eye = GetEye();
        if (eye == null)
            return;

        PlaceInFront(eye);
        SavePose(transform.position, transform.rotation);
    }

    private void PositionCanvas()
    {
        Transform eye = GetEye();

        if (eye == null)
        {
            Debug.LogError("MainMenuCanvasPositioner : Main Camera를 찾을 수 없습니다.");
            return;
        }

        if (hasSavedPose && CanReuse(eye))
        {
            transform.SetPositionAndRotation(savedPosition, savedRotation);
            Debug.Log($"{name} 위치 : 이전 메뉴 위치 사용 {transform.position}");
            return;
        }

        PlaceInFront(eye);
        SavePose(transform.position, transform.rotation);

        Debug.Log($"{name} 위치 : 정면에 새로 배치 {transform.position}");
    }

    private Transform GetEye()
    {
        if (centerEye != null && centerEye.gameObject.activeInHierarchy)
            return centerEye;

        Camera mainCamera = Camera.main;
        return mainCamera != null ? mainCamera.transform : null;
    }

    private bool CanReuse(Transform eye)
    {
        Vector3 toCanvas = savedPosition - eye.position;
        Vector3 flat = new Vector3(toCanvas.x, 0f, toCanvas.z);

        if (flat.magnitude > maxReuseDistance)
            return false;

        Vector3 forward = eye.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f || flat.sqrMagnitude < 0.001f)
            return true;

        return Vector3.Angle(forward, flat) <= maxReuseAngle;
    }

    private void PlaceInFront(Transform eye)
    {
        // 눈의 위치를 기준으로 정면 방향 계산 (위아래로 고개를 숙여도 메뉴는 눈높이에)
        Vector3 forward = eye.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        forward.Normalize();

        transform.position = eye.position + forward * distance;
        transform.rotation = FacingRotation(transform.position, eye.position);
    }

    /// <summary>Canvas가 사용자를 바라보도록 하는 회전 (수평 방향만)</summary>
    public static Quaternion FacingRotation(Vector3 canvasPosition, Vector3 eyePosition)
    {
        Vector3 away = canvasPosition - eyePosition;
        away.y = 0f;

        if (away.sqrMagnitude < 0.001f)
            away = Vector3.forward;

        // UI Canvas는 앞(forward)이 사용자 반대쪽을 향해야 정면으로 보인다
        return Quaternion.LookRotation(away.normalized, Vector3.up);
    }
}
