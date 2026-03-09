using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class MobAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Detection")]
    public float detectionRange = 7f;
    public float attackRange = 2f;
    public float loseAggroRange = 10f;

    [Header("Movement")]
    public float chaseSpeed = 4f;
    public float rotationSpeed = 10f;
    public float gravity = -20f;

    [Header("Combat")]
    public int comboCount = 3;
    public float delayBetweenAttacks = 0.4f;
    public float cooldownAfterCombo = 2.5f;

    [Header("Damage")]
    public int attackDamage = 15;
    public float hitRadius = 1.8f;
    public float hitForwardOffset = 1f;

    Animator animator;
    CharacterController controller;
    Health health;

    enum State { Idle, Chase, Attack, Cooldown }
    State currentState = State.Idle;
    int currentAttack;
    float stateTimer;
    float verticalVelocity;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");
    static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");

    void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        health = GetComponent<Health>();
    }

    void Start()
    {
        if (player == null)
        {
            var movement = FindObjectOfType<IsoCharacterMovement>();
            if (movement != null) player = movement.transform;
        }
    }

    void Update()
    {
        if (player == null) return;
        if (health != null && health.IsDead) return;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 horizontalMove = Vector3.zero;
        float dist = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Idle:     HandleIdle(dist);                          break;
            case State.Chase:    horizontalMove = HandleChase(dist);        break;
            case State.Attack:   HandleAttack();                            break;
            case State.Cooldown: HandleCooldown(dist);                      break;
        }

        Vector3 finalMove = horizontalMove;
        finalMove.y = verticalVelocity;
        controller.Move(finalMove * Time.deltaTime);
    }

    void HandleIdle(float dist)
    {
        animator.SetFloat(SpeedHash, 0f);

        if (dist <= detectionRange)
            currentState = State.Chase;
    }

    Vector3 HandleChase(float dist)
    {
        if (dist > loseAggroRange)
        {
            currentState = State.Idle;
            animator.SetFloat(SpeedHash, 0f);
            return Vector3.zero;
        }

        if (dist <= attackRange)
        {
            currentAttack = 0;
            currentState = State.Attack;
            FireAttack();
            return Vector3.zero;
        }

        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        dir.Normalize();

        RotateToward(dir);
        animator.SetFloat(SpeedHash, 1f);
        return dir * chaseSpeed;
    }

    void HandleAttack()
    {
        FacePlayer();
        animator.SetFloat(SpeedHash, 0f);
    }

    void HandleCooldown(float dist)
    {
        FacePlayer();
        animator.SetFloat(SpeedHash, 0f);

        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f) return;

        if (dist <= attackRange)
        {
            currentAttack = 0;
            currentState = State.Attack;
            FireAttack();
        }
        else if (dist <= detectionRange)
        {
            currentState = State.Chase;
        }
        else
        {
            currentState = State.Idle;
        }
    }

    void FireAttack()
    {
        animator.SetInteger(AttackIndexHash, currentAttack);
        animator.SetTrigger(AttackTriggerHash);
        animator.SetBool(IsAttackingHash, true);
    }

    // Animation Event: attack animasyonunun vurus anina ekle
    public void OnMobAttackHit()
    {
        if (player == null) return;

        Vector3 center = transform.position + transform.forward * hitForwardOffset + Vector3.up;
        float dist = Vector3.Distance(center, player.position);

        if (dist <= hitRadius)
        {
            var playerHealth = player.GetComponent<Health>();
            if (playerHealth != null && !playerHealth.IsDead)
                playerHealth.TakeDamage(attackDamage);
        }
    }

    // Animation Event: her attack klibinin sonuna ekle
    public void OnMobAttackEnd()
    {
        currentAttack++;

        if (currentAttack < comboCount)
        {
            Invoke(nameof(FireAttack), delayBetweenAttacks);
        }
        else
        {
            animator.SetBool(IsAttackingHash, false);
            currentState = State.Cooldown;
            stateTimer = cooldownAfterCombo;
        }
    }

    void FacePlayer()
    {
        if (player == null) return;
        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
            RotateToward(dir.normalized);
    }

    void RotateToward(Vector3 dir)
    {
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            rotationSpeed * Time.deltaTime);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, loseAggroRange);
    }
}
