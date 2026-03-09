using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(WeaponHolder))]
public class ComboAttack : MonoBehaviour
{
    public int maxCombo = 3;
    public float comboResetTime = 1.2f;

    [Header("Damage")]
    public int attackDamage = 10;
    public float hitRadius = 1.5f;
    public float hitForwardOffset = 1f;

    private Animator animator;
    private WeaponHolder weaponHolder;

    private int currentCombo;
    private float lastAttackTime;
    private bool attackQueued;
    private bool isAttacking;

    static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");
    static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");

    void Awake()
    {
        animator = GetComponent<Animator>();
        weaponHolder = GetComponent<WeaponHolder>();
    }

    void Update()
    {
        if (Time.time - lastAttackTime > comboResetTime && !isAttacking)
            currentCombo = 0;

        if (Input.GetMouseButtonDown(0))
        {
            if (!isAttacking)
                StartAttack();
            else
                attackQueued = true;
        }
    }

    void StartAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        animator.SetInteger(AttackIndexHash, currentCombo);
        animator.SetTrigger(AttackTriggerHash);
        animator.SetBool(IsAttackingHash, true);

        weaponHolder.SetTrailActive(true);
    }

    // Animation Event: attack animasyonunun vurus anina ekle
    public void OnAttackHit()
    {
        Vector3 center = transform.position + transform.forward * hitForwardOffset + Vector3.up;
        Collider[] hits = Physics.OverlapSphere(center, hitRadius);

        foreach (var hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            var health = hit.GetComponentInParent<Health>();
            if (health != null && !health.IsDead)
            {
                health.TakeDamage(attackDamage);
                break;
            }
        }
    }

    // Animation Event: her attack animasyonunun sonuna ekle
    public void OnAttackEnd()
    {
        if (attackQueued && currentCombo < maxCombo - 1)
        {
            attackQueued = false;
            currentCombo++;
            StartAttack();
        }
        else
        {
            isAttacking = false;
            attackQueued = false;
            currentCombo = 0;
            animator.SetBool(IsAttackingHash, false);
            weaponHolder.SetTrailActive(false);
        }
    }
}
