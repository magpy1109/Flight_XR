using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 4f;
    public float mouseSensitivity = 2f;
    public float gravity = -9.81f;

    private CharacterController controller;
    private float verticalVelocity;
    private float pitch;

    public Transform cameraTransform;

    public float jumpHeight = 1.5f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked; // 커서 숨기고 화면 중앙에 고정 (시점 조작용)
    }

    void Update()
    {
        Move();
        // 🌟 마우스 시점 조작(Look)은 비행기가 날아가든 말든 항상 실행되게 그대로 둡니다!
        Look(); 
    }

    void Move()
    {
        // 🌟 씬에 살아서 날아가고 있는 비행기가 있는지 실시간으로 확인합니다!
        PaperPlaneFlight activePlane = FindFirstObjectByType<PaperPlaneFlight>();
        
        // activePlane이 존재하고, enabled 상태(아직 안 부딪힘)라면 true가 됩니다.
        bool isPlaneFlying = (activePlane != null && activePlane.enabled);

        float x = 0f;
        float z = 0f;

        // 🌟 비행기가 안 날아갈 때만 플레이어가 WASD로 움직일 수 있습니다! (발 묶기)
        if (!isPlaneFlying)
        {
            x = Input.GetAxis("Horizontal");
            z = Input.GetAxis("Vertical");
        }

        Vector3 move = transform.right * x + transform.forward * z;

        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }

        // 비행기가 안 날아갈 때만 점프(Space)도 가능하게 막아줍니다.
        if (Input.GetButtonDown("Jump") && controller.isGrounded && !isPlaneFlying)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        move.y = verticalVelocity;

        controller.Move(move * moveSpeed * Time.deltaTime);
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -80f, 80f);

        cameraTransform.localRotation = Quaternion.Euler(pitch, 0, 0);
    }
}