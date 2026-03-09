using UnityEngine;

[RequireComponent(typeof(Animator))]
public class WeaponHolder : MonoBehaviour
{
    public Vector3 gripPositionOffset;
    public Vector3 gripRotationOffset;

    private Transform handBone;
    private GameObject currentWeapon;
    private Animator animator;
    private TrailRenderer[] trails;

    static readonly int IsArmedHash = Animator.StringToHash("IsArmed");

    void Start()
    {
        animator = GetComponent<Animator>();
        handBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
    }

    public void Equip(GameObject weaponPrefab)
    {
        if (currentWeapon != null)
            Destroy(currentWeapon);

        currentWeapon = Instantiate(weaponPrefab, handBone);
        currentWeapon.transform.localPosition = gripPositionOffset;
        currentWeapon.transform.localRotation = Quaternion.Euler(gripRotationOffset);

        trails = currentWeapon.GetComponentsInChildren<TrailRenderer>(true);
        SetTrailActive(false);

        animator.SetBool(IsArmedHash, true);
    }

    public void Unequip()
    {
        if (currentWeapon != null)
        {
            Destroy(currentWeapon);
            currentWeapon = null;
        }

        trails = null;
        animator.SetBool(IsArmedHash, false);
    }

    public void SetTrailActive(bool active)
    {
        if (trails == null) return;
        foreach (var trail in trails)
        {
            if (trail == null) continue;
            trail.emitting = active;
            if (!active) trail.Clear();
        }
    }

    public bool HasWeapon => currentWeapon != null;
}
