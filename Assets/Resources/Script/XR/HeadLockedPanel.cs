using UnityEngine;

/// <summary>
/// 월드 Canvas를 HUD처럼 사용자 시야에 고정한다. (고개를 돌리면 따라온다)
/// 일시정지 화면 / 게임 결과 화면에 자동으로 붙는다.
///
/// - 눈앞 distance(m)에 항상 정면을 보도록 배치
/// - 완전히 딱 붙으면 어지러울 수 있어 아주 짧게 부드럽게 따라온다 (followSpeed)
/// - 일시정지(Time.timeScale = 0) 중에도 동작
/// </summary>
public class HeadLockedPanel : MonoBehaviour
{
    [Tooltip("눈에서 떨어진 거리 (m)")]
    public float distance = 1.5f;

    [Tooltip("눈높이 기준 위아래 위치 (m, 음수 = 아래)")]
    public float heightOffset = -0.05f;

    [Tooltip("따라오는 빠르기 (클수록 딱 붙음, 0이면 즉시)")]
    public float followSpeed = 14f;

    private Transform eye;

    /// <summary>캔버스에 붙이고 설정 (이미 있으면 그대로 사용)</summary>
    public static HeadLockedPanel Attach(GameObject target, float distance)
    {
        HeadLockedPanel panel = target.GetComponent<HeadLockedPanel>();
        if (panel == null)
            panel = target.AddComponent<HeadLockedPanel>();

        panel.distance = distance;
        return panel;
    }

    private void OnEnable()
    {
        // 켜지는 순간 바로 정면에 (날아오는 느낌 없이)
        SnapToView();
    }

    private void LateUpdate()
    {
        if (!TryGetTarget(out Vector3 position, out Quaternion rotation))
            return;

        if (followSpeed <= 0f)
        {
            transform.SetPositionAndRotation(position, rotation);
            return;
        }

        float t = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
        transform.SetPositionAndRotation(
            Vector3.Lerp(transform.position, position, t),
            Quaternion.Slerp(transform.rotation, rotation, t));
    }

    public void SnapToView()
    {
        if (TryGetTarget(out Vector3 position, out Quaternion rotation))
            transform.SetPositionAndRotation(position, rotation);
    }

    private bool TryGetTarget(out Vector3 position, out Quaternion rotation)
    {
        if (eye == null || !eye.gameObject.activeInHierarchy)
        {
            Camera cam = Camera.main;
            eye = cam != null ? cam.transform : null;
        }

        if (eye == null)
        {
            position = transform.position;
            rotation = transform.rotation;
            return false;
        }

        // 시야 정면 (고개를 숙이거나 들면 같이 따라옴)
        position = eye.position + eye.forward * distance + eye.up * heightOffset;

        // UI Canvas는 forward가 사용자 반대쪽을 향해야 정면으로 보인다
        rotation = Quaternion.LookRotation(eye.forward, eye.up);
        return true;
    }
}
