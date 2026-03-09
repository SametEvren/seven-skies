using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    public GameObject hitParticlePrefab;
    public GameObject deathParticlePrefab;
    public float particleLifetime = 2f;
    public Vector3 particleOffset = new Vector3(0, 1f, 0);
    public bool destroyOnDeath = true;
    public float deathDelay = 1.5f;

    int currentHealth;
    bool isDead;

    public bool IsDead => isDead;
    public bool IsInvulnerable { get; set; }
    public int CurrentHealth => currentHealth;
    public event Action OnDeath;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (isDead || IsInvulnerable) return;

        currentHealth -= amount;
        SpawnParticle(hitParticlePrefab);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        SpawnParticle(deathParticlePrefab);
        OnDeath?.Invoke();

        if (destroyOnDeath)
            Destroy(gameObject, deathDelay);
    }

    void SpawnParticle(GameObject prefab)
    {
        if (prefab == null) return;
        var fx = Instantiate(prefab, transform.position + particleOffset, Quaternion.identity);
        Destroy(fx, particleLifetime);
    }
}
