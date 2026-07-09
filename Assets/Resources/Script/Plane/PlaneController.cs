using System;
using UnityEngine;

[RequireComponent(typeof(PlanePhysics))]
public class PlaneController : MonoBehaviour
{
    public event Action OnPlaneDestroyed;

    private PlanePhysics physics;

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
    }

    private void OnDestroy()
    {
        OnPlaneDestroyed?.Invoke();
    }
}