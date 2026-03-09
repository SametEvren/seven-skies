using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class BossAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public GameObject fireballPrefab;

    [Header("Arena")]
    public float arenaRadius = 20f;

    [Header("Detection")]
    public float activationRange = 15f;
    public float loseAggroRange = 30f;

    [Header("General")]
    public float rotationSpeed = 8f;
    public float gravity = -20f;
    public float cooldownBetweenSkills = 3f;

    [Header("Skill 1 – Crawl Rush")]
    public float crawlSpeed = 14f;
    public float crawlOrbitTime = 2f;
    public float crawlChargeTime = 1.2f;
    public float crawlChargeSpeed = 24f;
    public int crawlDamage = 20;
    public float crawlHitRadius = 2f;

    [Header("Skill 2 – Run Rush")]
    public float runSpeed = 18f;
    public float runOrbitTime = 1.5f;
    public float runChargeTime = 1f;
    public float runChargeSpeed = 28f;
    public int runDamage = 30;
    public float runHitRadius = 2.5f;

    [Header("Rush Orbit")]
    public float orbitMinRadius = 4f;
    public float orbitMaxRadius = 8f;
    public float orbitAngularSpeed = 120f;
    public float telegraphDuration = 0.4f;

    [Header("Skill 3 – Mage Fireball")]
    public float castDuration = 3f;
    public int fireballCount = 5;
    public float fireballInterval = 0.5f;

    Animator animator;
    CharacterController controller;
    Health health;

    enum State { Idle, PickSkill, CrawlRush, RunRush, MageCast, Cooldown }
    State state = State.Idle;

    enum RushPhase { Orbit, Telegraph, Charge }
    RushPhase rushPhase;

    float stateTimer;
    float phaseTimer;
    float subTimer;
    int fireballsFired;
    float verticalVelocity;
    bool chargeDamageDealt;

    float orbitAngle;
    float orbitDirection;
    float noiseOffset;
    Vector3 chargeDirection;
    Vector3 spawnCenter;

    List<int> skillBag = new List<int>();

    static readonly int SkillIndexHash = Animator.StringToHash("SkillIndex");
    static readonly int UseSkillHash = Animator.StringToHash("UseSkill");
    static readonly int IsUsingSkillHash = Animator.StringToHash("IsUsingSkill");

    void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        health = GetComponent<Health>();
    }

    void Start()
    {
        spawnCenter = transform.position;

        if (player == null)
        {
            var pm = FindObjectOfType<IsoCharacterMovement>();
            if (pm != null) player = pm.transform;
        }
        RefillSkillBag();
    }

    void Update()
    {
        if (player == null) return;
        if (health != null && health.IsDead) return;

        ApplyGravity();

        Vector3 move = Vector3.zero;
        float dist = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case State.Idle:      HandleIdle(dist);              break;
            case State.PickSkill: HandlePickSkill(dist);         break;
            case State.CrawlRush: move = HandleRush(dist, crawlSpeed, crawlChargeSpeed, crawlOrbitTime, crawlChargeTime, crawlDamage, crawlHitRadius); break;
            case State.RunRush:   move = HandleRush(dist, runSpeed, runChargeSpeed, runOrbitTime, runChargeTime, runDamage, runHitRadius);               break;
            case State.MageCast:  HandleMageCast();              break;
            case State.Cooldown:  HandleCooldown(dist);          break;
        }

        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);

        ClampToArena();
    }

    void ClampToArena()
    {
        Vector3 offset = transform.position - spawnCenter;
        offset.y = 0;
        if (offset.magnitude > arenaRadius)
        {
            Vector3 clamped = spawnCenter + offset.normalized * arenaRadius;
            clamped.y = transform.position.y;
            controller.enabled = false;
            transform.position = clamped;
            controller.enabled = true;
        }
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
    }

    void HandleIdle(float dist)
    {
        if (dist <= activationRange)
        {
            state = State.PickSkill;
            stateTimer = 0.5f;
        }
    }

    void HandlePickSkill(float dist)
    {
        FacePlayer();

        if (dist > loseAggroRange)
        {
            state = State.Idle;
            return;
        }

        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f) return;

        if (skillBag.Count == 0)
            RefillSkillBag();

        int idx = Random.Range(0, skillBag.Count);
        int skill = skillBag[idx];
        skillBag.RemoveAt(idx);

        animator.SetInteger(SkillIndexHash, skill);
        animator.SetTrigger(UseSkillHash);
        animator.SetBool(IsUsingSkillHash, true);

        switch (skill)
        {
            case 0:
                state = State.CrawlRush;
                InitRush(crawlOrbitTime);
                break;
            case 1:
                state = State.RunRush;
                InitRush(runOrbitTime);
                break;
            case 2:
                state = State.MageCast;
                stateTimer = castDuration;
                fireballsFired = 0;
                subTimer = 0f;
                break;
        }
    }

    void InitRush(float orbitTime)
    {
        rushPhase = RushPhase.Orbit;
        phaseTimer = orbitTime;
        chargeDamageDealt = false;

        Vector3 offset = transform.position - player.position;
        offset.y = 0;
        orbitAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
        orbitDirection = Random.value > 0.5f ? 1f : -1f;
        noiseOffset = Random.Range(0f, 100f);
    }

    Vector3 HandleRush(float dist, float orbitSpd, float chargeSpd, float orbitTime, float chargeTime, int dmg, float hitRadius)
    {
        phaseTimer -= Time.deltaTime;

        switch (rushPhase)
        {
            case RushPhase.Orbit:
            {
                if (phaseTimer <= 0f)
                {
                    rushPhase = RushPhase.Telegraph;
                    phaseTimer = telegraphDuration;
                    return Vector3.zero;
                }

                Vector3 moveDir = CalculateOrbitMove();
                RotateToward(moveDir);
                return moveDir * orbitSpd;
            }

            case RushPhase.Telegraph:
            {
                FacePlayer();
                if (phaseTimer <= 0f)
                {
                    rushPhase = RushPhase.Charge;
                    phaseTimer = chargeTime;
                    chargeDirection = DirectionToPlayer();
                }
                return Vector3.zero;
            }

            case RushPhase.Charge:
            {
                RotateToward(chargeDirection);

                if (!chargeDamageDealt && dist <= hitRadius)
                {
                    DamagePlayer(dmg);
                    chargeDamageDealt = true;
                }

                if (phaseTimer <= 0f)
                {
                    EndSkill();
                    return Vector3.zero;
                }

                return chargeDirection * chargeSpd;
            }
        }

        return Vector3.zero;
    }

    Vector3 CalculateOrbitMove()
    {
        float noise1 = Mathf.PerlinNoise(Time.time * 0.6f + noiseOffset, 0f);
        float noise2 = Mathf.PerlinNoise(0f, Time.time * 0.4f + noiseOffset);

        float angularSpeed = orbitAngularSpeed * (0.6f + noise1 * 0.8f) * orbitDirection;
        orbitAngle += angularSpeed * Time.deltaTime;

        if (Random.value < 0.005f)
            orbitDirection *= -1f;

        float radius = Mathf.Lerp(orbitMinRadius, orbitMaxRadius, noise2);

        float rad = orbitAngle * Mathf.Deg2Rad;
        Vector3 targetPos = player.position + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * radius;
        targetPos.y = transform.position.y;

        Vector3 dir = targetPos - transform.position;
        dir.y = 0;
        return dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;
    }

    void HandleMageCast()
    {
        FacePlayer();

        stateTimer -= Time.deltaTime;
        subTimer -= Time.deltaTime;

        if (fireballsFired < fireballCount && subTimer <= 0f)
        {
            SpawnFireball();
            fireballsFired++;
            subTimer = fireballInterval;
        }

        if (stateTimer <= 0f)
            EndSkill();
    }

    void HandleCooldown(float dist)
    {
        FacePlayer();

        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f) return;

        if (dist > loseAggroRange)
            state = State.Idle;
        else
        {
            state = State.PickSkill;
            stateTimer = 0.3f;
        }
    }

    void EndSkill()
    {
        animator.SetBool(IsUsingSkillHash, false);
        state = State.Cooldown;
        stateTimer = cooldownBetweenSkills;
    }

    void SpawnFireball()
    {
        if (fireballPrefab == null || player == null) return;

        Vector2 rnd = Random.insideUnitCircle * 1.5f;
        Vector3 spawnPos = transform.position + transform.forward * 1.5f;
        spawnPos += new Vector3(rnd.x, 0f, rnd.y);

        GameObject go = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);

        var fb = go.GetComponent<Fireball>();
        if (fb == null)
            fb = go.AddComponent<Fireball>();

        fb.SetTarget(player);
    }

    void DamagePlayer(int amount)
    {
        if (player == null) return;
        var hp = player.GetComponent<Health>();
        if (hp != null && !hp.IsDead)
            hp.TakeDamage(amount);
    }

    void RefillSkillBag()
    {
        skillBag.Clear();
        skillBag.AddRange(new[] { 0, 1, 2 });
    }

    Vector3 DirectionToPlayer()
    {
        Vector3 d = player.position - transform.position;
        d.y = 0;
        return d.sqrMagnitude > 0.001f ? d.normalized : transform.forward;
    }

    void FacePlayer()
    {
        if (player == null) return;
        Vector3 d = player.position - transform.position;
        d.y = 0;
        if (d.sqrMagnitude > 0.001f)
            RotateToward(d.normalized);
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
        Vector3 center = Application.isPlaying ? spawnCenter : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, arenaRadius);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, activationRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, loseAggroRange);
    }
}
