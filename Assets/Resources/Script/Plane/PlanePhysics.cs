using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlanePhysics : MonoBehaviour
{
    private Rigidbody rb;

    [Header("Speed")]
    public float forwardForce = 3f;

    [Header("Turn")]
    public float turnSpeed = 90f;

    [Header("Lift")]
    public float liftForce = 5f;

    float currentTurn;
    float currentLift;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetTurn(float value)
    {
        currentTurn = value;
    }

    public void SetLift(float value)
    {
        currentLift = value;
    }

    void FixedUpdate()
    {
        // 항상 전진
        rb.AddForce(
            transform.forward * forwardForce,
            ForceMode.Acceleration);

        // 회전
        rb.MoveRotation(
            rb.rotation *
            Quaternion.Euler(
                0,
                currentTurn * turnSpeed * Time.fixedDeltaTime,
                0));

        // 상승
        rb.AddForce(
            Vector3.up * currentLift * liftForce,
            ForceMode.Acceleration);
    }
}