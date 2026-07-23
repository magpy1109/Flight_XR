using System;
using UnityEngine;

[RequireComponent(typeof(PlanePhysics))]
public class PlaneController : MonoBehaviour
{
    public event Action OnPlaneDestroyed;

    private PlanePhysics physics;

    private Vector3 startPosition;

    void Awake()
    {
        physics = GetComponent<PlanePhysics>();
        startPosition = transform.position;
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
            float distance =
                Vector3.Distance(startPosition, transform.position);

            GameManager.Instance.UpdateDistance(distance);

            GameManager.Instance.UpdateHeight(transform.position.y);
        }
    }

    private void OnDestroy()
    {
        OnPlaneDestroyed?.Invoke();
    }

    public void StartFlight()
    {
        startPosition = transform.position;
    }
}