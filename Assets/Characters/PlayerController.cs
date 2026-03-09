using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 6f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("Camera Settings")]
    public Transform cameraPivot;
    public float mouseSensitivity = 0.2f;

    [Header("Animator")]
    public Animator animator; // Inspector'dan atayacaðýz

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private float cameraPitch = 0f;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpInput;

    // Yeni: gerçek hareketin hesaplanmasý
    private Vector3 lastPosition;
    private float currentSpeed;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        lastPosition = transform.position;
    }

    void Update()
    {
        ReadInput();
        Move();
        CameraLook();
        ApplyGravity();
        Jump();
        CalculateSpeed();
        UpdateAnimator();
    }

    void ReadInput()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        moveInput = Vector2.zero;

        if (keyboard.wKey.isPressed) moveInput.y += 1;
        if (keyboard.sKey.isPressed) moveInput.y -= 1;
        if (keyboard.aKey.isPressed) moveInput.x -= 1;
        if (keyboard.dKey.isPressed) moveInput.x += 1;

        lookInput = mouse.delta.ReadValue() * mouseSensitivity;
        jumpInput = keyboard.spaceKey.wasPressedThisFrame;
    }

    void Move()
    {
        Vector3 move = transform.forward * moveInput.y + transform.right * moveInput.x;
        controller.Move(move * speed * Time.deltaTime);
    }

    void CameraLook()
    {
        float mouseX = lookInput.x;
        float mouseY = lookInput.y;

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -40f, 70f);

        cameraPivot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    void ApplyGravity()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);
    }

    void Jump()
    {
        if (jumpInput && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void CalculateSpeed()
    {
        // Gerçek hareket vektörü (CharacterController ile olan pozisyon deðiþimi)
        Vector3 delta = transform.position - lastPosition;
        delta.y = 0; // Yalnýzca yatay hareket
        currentSpeed = delta.magnitude / Time.deltaTime;
        lastPosition = transform.position;
    }

    void UpdateAnimator()
    {
        // Speed artýk gerçek hareketten geliyor
        animator.SetFloat("Speed", currentSpeed);

        animator.SetBool("IsJumping", !isGrounded && velocity.y > 0);
        animator.SetBool("IsGrounded", isGrounded);
    }
}