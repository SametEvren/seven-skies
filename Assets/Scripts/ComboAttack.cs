using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(WeaponHolder))]
public class ComboAttack : MonoBehaviour
{
    public int maxCombo = 3;
    public float comboResetTime = 1.2f;

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
    }

    // Animation Event: her attack animasyonunun sonuna eklenecek
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
        }
    }
}
