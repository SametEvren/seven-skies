using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class IsoCharacterMovement : MonoBehaviour
{
    public float walkSpeed = 3.5f;
    public float runSpeed = 7f;
    public float gravity = -20f;
    public float animSmoothTime = 0.1f;
    public Transform cameraTransform;

    [Header("Dash")]
    public float dashSpeed = 22f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public GameObject dashEffectPrefab;
    public float dashEffectLifetime = 1f;

    private CharacterController controller;
    private Animator animator;
    private Health health;
    private float verticalVelocity;
    private float currentAnimSpeed;
    private float animSpeedVelocity;

    private ComboAttack comboAttack;

    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector3 dashDirection;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        comboAttack = GetComponent<ComboAttack>();
        health = GetComponent<Health>();
    }

    bool IsAttacking => comboAttack != null && animator.GetBool("IsAttacking");

    void Update()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        dashCooldownTimer -= Time.deltaTime;

        Vector3 horizontalVelocity = Vector3.zero;
        float targetAnimSpeed = 0f;

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            horizontalVelocity = dashDirection * dashSpeed;

            if (dashTimer <= 0f)
                EndDash();
        }
        else if (!IsAttacking)
        {
            if (Input.GetKeyDown(KeyCode.Space) && dashCooldownTimer <= 0f)
            {
                StartDash();
                horizontalVelocity = dashDirection * dashSpeed;
            }
            else
            {
                float h = Input.GetAxis("Horizontal");
                float v = Input.GetAxis("Vertical");
                bool wantsRun = Input.GetKey(KeyCode.LeftShift);

                Vector3 camForward = cameraTransform.forward;
                Vector3 camRight = cameraTransform.right;
                camForward.y = 0;
                camRight.y = 0;
                camForward.Normalize();
                camRight.Normalize();

                Vector3 moveDirection = camForward * v + camRight * h;
                float inputMag = Mathf.Clamp01(moveDirection.magnitude);

                if (inputMag > 0.1f)
                {
                    moveDirection.Normalize();
                    transform.forward = moveDirection;
                    float speed = wantsRun ? runSpeed : walkSpeed;
                    horizontalVelocity = moveDirection * speed;
                    targetAnimSpeed = wantsRun ? 1f : 0.5f;
                }
            }
        }

        currentAnimSpeed = Mathf.SmoothDamp(currentAnimSpeed, targetAnimSpeed, ref animSpeedVelocity, animSmoothTime);
        animator.SetFloat(SpeedHash, currentAnimSpeed);
        animator.SetBool(IsGroundedHash, controller.isGrounded);

        Vector3 finalVelocity = horizontalVelocity;
        finalVelocity.y = verticalVelocity;
        controller.Move(finalVelocity * Time.deltaTime);
    }

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        dashDirection = transform.forward;

        if (health != null)
            health.IsInvulnerable = true;

        if (dashEffectPrefab != null)
        {
            var fx = Instantiate(dashEffectPrefab, transform.position, transform.rotation, transform);
            Destroy(fx, dashEffectLifetime);
        }
    }

    void EndDash()
    {
        isDashing = false;

        if (health != null)
            health.IsInvulnerable = false;
    }
}
