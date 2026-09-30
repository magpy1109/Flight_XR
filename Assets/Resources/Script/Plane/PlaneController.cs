using System;
using UnityEngine;

[RequireComponent(typeof(PlanePhysics))]
public class PlaneController : MonoBehaviour
{
    public event Action OnPlaneDestroyed;

    private PlanePhysics physics;

    // 비행 거리 = 실제로 날아간 경로의 길이 (수평 이동 합계)
    // 예전에는 출발점에서의 직선거리라서, 한 자리에서 빙빙 돌면 거리가 늘지 않았음
    private Vector3 lastPosition;
    private float flownDistance;

    void Awake()
    {
        physics = GetComponent<PlanePhysics>();
    }

    void Update()
    {
        physics.SetTurn(
            FlightInputManager.Instance.TurnInput);

        physics.SetLift(
            FlightInputManager.Instance.BlowInput);

        if (GameManager.Instance != null &&
            GameManager.Instance.IsPlaying)
        {
            Vector3 position = transform.position;

            Vector3 step = position - lastPosition;
            step.y = 0f;   // 위아래 움직임은 거리에서 제외 (앞으로 날아간 만큼만)

            flownDistance += step.magnitude;
            lastPosition = position;

            GameManager.Instance.UpdateDistance(flownDistance);

            GameManager.Instance.UpdateHeight(position.y);
        }
        else
        {
            lastPosition = transform.position;
        }
    }

    private void OnDestroy()
    {
        OnPlaneDestroyed?.Invoke();
    }

    public void StartFlight()
    {
        lastPosition = transform.position;
        flownDistance = 0f;
    }
}
