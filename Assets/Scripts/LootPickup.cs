using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class LootPickup : MonoBehaviour
{
    public GameObject weaponPrefab;
    public bool destroyOnPickup = true;

    void Reset()
    {
        var col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.5f;
    }

    void OnTriggerEnter(Collider other)
    {
        var holder = other.GetComponentInParent<WeaponHolder>();
        if (holder == null)
            return;

        holder.Equip(weaponPrefab);

        if (destroyOnPickup)
            Destroy(gameObject);
    }
}
