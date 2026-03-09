using UnityEngine;

public class Fireball : MonoBehaviour
{
    [Header("Emerge Phase")]
    public float riseHeight = 2.5f;
    public float riseDuration = 0.5f;

    [Header("Flight Phase")]
    public float flySpeed = 14f;
    public float homingStrength = 4f;
    public float lifetime = 8f;

    [Header("Damage")]
    public int damage = 20;
    public float damageRadius = 1.2f;

    [Header("Effects")]
    public GameObject impactEffectPrefab;

    Transform target;
    Vector3 flyDirection;
    Vector3 startPos;
    float timer;
    bool isFlying;
    bool initialized;

    public void SetTarget(Transform t)
    {
        target = t;
    }

    void Start()
    {
        Init();
    }

    void Init()
    {
        if (initialized) return;
        initialized = true;

        startPos = transform.position;
        Destroy(gameObject, lifetime);

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void Update()
    {
        Init();

        timer += Time.deltaTime;

        if (!isFlying)
        {
            float t = Mathf.Clamp01(timer / riseDuration);
            transform.position = startPos + Vector3.up * riseHeight * t;

            if (t >= 1f)
            {
                isFlying = true;

                if (target != null)
                    flyDirection = (target.position + Vector3.up - transform.position).normalized;
                else
                    flyDirection = transform.forward;
            }
        }
        else
        {
            if (target != null)
            {
                Vector3 desired = (target.position + Vector3.up - transform.position).normalized;
                flyDirection = Vector3.Slerp(flyDirection, desired, homingStrength * Time.deltaTime).normalized;
            }

            transform.position += flyDirection * flySpeed * Time.deltaTime;

            if (flyDirection.sqrMagnitude > 0.001f)
                transform.forward = flyDirection;

            CheckHit();
        }
    }

    void CheckHit()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, damageRadius);
        foreach (var hit in hits)
        {
            if (hit.GetComponentInParent<IsoCharacterMovement>() == null)
                continue;

            var hp = hit.GetComponentInParent<Health>();
            if (hp != null && !hp.IsDead)
            {
                hp.TakeDamage(damage);
                SpawnImpact();
                Destroy(gameObject);
                return;
            }
        }
    }

    void SpawnImpact()
    {
        if (impactEffectPrefab == null) return;
        var fx = Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        Destroy(fx, 2f);
    }
}
