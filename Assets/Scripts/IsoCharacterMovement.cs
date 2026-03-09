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

    private CharacterController controller;
    private Animator animator;
    private float verticalVelocity;
    private float currentAnimSpeed;
    private float animSpeedVelocity;

    private ComboAttack comboAttack;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        comboAttack = GetComponent<ComboAttack>();
    }

    bool IsAttacking => comboAttack != null && animator.GetBool("IsAttacking");

    void Update()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 horizontalVelocity = Vector3.zero;
        float targetAnimSpeed = 0f;

        if (!IsAttacking)
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

        currentAnimSpeed = Mathf.SmoothDamp(currentAnimSpeed, targetAnimSpeed, ref animSpeedVelocity, animSmoothTime);
        animator.SetFloat(SpeedHash, currentAnimSpeed);
        animator.SetBool(IsGroundedHash, controller.isGrounded);

        Vector3 finalVelocity = horizontalVelocity;
        finalVelocity.y = verticalVelocity;
        controller.Move(finalVelocity * Time.deltaTime);
    }
}
