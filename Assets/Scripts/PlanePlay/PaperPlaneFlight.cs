using UnityEngine;
using System.Collections;
using TMPro;

public class PaperPlaneFlight : MonoBehaviour
{
    [Header("Flight Settings")]
    public float forwardForce = 2.0f;       
    public float turnSpeed = 100.0f;        
    public float speedBoost = 1.5f;         
    
    [Header("Launch Settings")]
    public float initialLaunchSpeed = 4.0f; 
    public float antiGravityTime = 1.0f;     

    [Header("Breath & Gravity Settings (💡 사이다 상승 패치!)")]
    public float customGravity = 3.0f;    
    // 🌟 위로 밀어 올리는 힘을 8.0 -> 15.0으로 2배 가까이 뻥튀기!
    public float maxLiftForce = 20.0f;    
    public float maxBreathInput = 1000f; 

    [Header("🌟 Nose Auto-Leveling Settings")]
    public float pitchCorrectSpeed = 2.0f; 

    private Rigidbody rb;
    private BreathDetector breathDetector;
    private TextMeshProUGUI countdownText;

    private enum FlightState { Countdown, WaitingForStart, Flying }
    private FlightState currentState = FlightState.Countdown;
    private bool isCustomGravityActive = false; 

    public bool IsFlying()
    {
        return currentState == FlightState.Flying;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearDamping = 1.8f; 
        rb.angularDamping = 2.0f; 
        
        breathDetector = FindFirstObjectByType<BreathDetector>();

        GameObject textObj = GameObject.Find("CountdownText");
        if (textObj != null)
        {
            countdownText = textObj.GetComponent<TextMeshProUGUI>();
        }

        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        if (countdownText != null) countdownText.text = "3";
        yield return new WaitForSeconds(1f);

        if (countdownText != null) countdownText.text = "2";
        yield return new WaitForSeconds(1f);

        if (countdownText != null) countdownText.text = "1";
        yield return new WaitForSeconds(1f);
        
        if (countdownText != null) countdownText.text = "START!\n<size=50>(WASD를 누르세요)</size>";
        currentState = FlightState.WaitingForStart; 
    }

    void Update()
    {
        if (currentState == FlightState.WaitingForStart)
        {
            if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f)
            {
                StartFlying();
            }
        }
    }

    void StartFlying()
    {
        if (countdownText != null) countdownText.text = ""; 
        currentState = FlightState.Flying;
        
        rb.linearVelocity = transform.forward * initialLaunchSpeed;
        StartCoroutine(AntiGravityRoutine());
    }

    IEnumerator AntiGravityRoutine()
    {
        isCustomGravityActive = false;
        yield return new WaitForSeconds(antiGravityTime);
        if (currentState == FlightState.Flying)
        {
            isCustomGravityActive = true; 
        }
    }

    void FixedUpdate()
    {
        if (currentState != FlightState.Flying) return;

        // 1. WASD 조종
        float turnInput = Input.GetAxis("Horizontal"); 
        float speedInput = Input.GetAxis("Vertical");   

        if (Mathf.Abs(turnInput) > 0.1f)
        {
            Quaternion turnRotation = Quaternion.Euler(0f, turnInput * turnSpeed * Time.fixedDeltaTime, 0f);
            rb.MoveRotation(rb.rotation * turnRotation);
        }

        float currentThrust = forwardForce + (speedInput * speedBoost);
        rb.AddForce(transform.forward * currentThrust, ForceMode.Acceleration);

        // 2. 종이비행기 전용 가짜 중력
        if (isCustomGravityActive)
        {
            rb.AddForce(Vector3.down * customGravity, ForceMode.Acceleration);
        }

        // 3. 🌬️ 입김 조종 (완벽하게 고쳐진 상승 로직)
        if (breathDetector != null)
        {
            float currentBreath = Mathf.Clamp(breathDetector.currentVolume, 0f, maxBreathInput);
            float breathPercent = currentBreath / maxBreathInput;

            // 입김을 살짝(5% 이상)이라도 불기 시작하면!
            if (breathPercent > 0.05f) 
            {
                // 🌟 [핵심 사이다 공식!]
                // 입김을 부는 순간 무조건 중력(customGravity)을 100% 상쇄해서 더 이상 안 떨어지게 잡아주고,
                // 거기에 네 입김 파워(maxLiftForce * percent)를 더해서 위로 밀어 올립니다!
                float calculatedLift = customGravity + (maxLiftForce * breathPercent);
                rb.AddForce(Vector3.up * calculatedLift, ForceMode.Acceleration);

                // 고개 수평 맞추기
                float pitchDot = Vector3.Dot(transform.forward, Vector3.up);

                if (pitchDot < -0.01f) 
                {
                    float repairForce = Mathf.Abs(pitchDot) * pitchCorrectSpeed * breathPercent;
                    rb.AddTorque(transform.right * repairForce, ForceMode.Acceleration);
                }
                else if (pitchDot > 0.1f)
                {
                    if (rb.angularVelocity.x < 0) 
                    {
                         rb.angularVelocity = new Vector3(rb.angularVelocity.x * 0.5f, rb.angularVelocity.y, rb.angularVelocity.z);
                    }
                }
            }
        }
    }
}