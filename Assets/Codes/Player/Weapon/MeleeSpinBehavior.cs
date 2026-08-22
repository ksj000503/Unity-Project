using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeSpinBehavior : IWeaponBehavior
{
    private Weapon owner;

    private WeaponData data;

    private Transform tf;

    private readonly HashSet<IDamageable> hitThisSpin = new HashSet<IDamageable>();

    public void Initialize(Weapon owner, WeaponData data)
    {
        this.owner = owner;

        this.data = data;

        this.tf = owner.transform;
    }

    public IEnumerator Execute(Transform target)
    {
        hitThisSpin.Clear();

        float duration = Mathf.Max(0.01f, data.spinDuration);

        float radius = Mathf.Max(0.01f, data.spinRadius);

        LayerMask mask = owner.EnemyMask;

        float startZ = tf.localEulerAngles.z;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            tf.localRotation = Quaternion.Euler(0f, 0f, startZ + 360f * t);

            Collider2D[] hits = Physics2D.OverlapCircleAll(tf.position, radius, mask);

            foreach (var h in hits)
            {
                if (h == null) continue;

                var dmg = h.GetComponent<IDamageable>();

                if (dmg == null) continue;

                if (hitThisSpin.Contains(dmg)) continue;

                hitThisSpin.Add(dmg);

                dmg.TakeDamage(owner.Damage);
            }

            yield return null;
        }

        tf.localRotation = Quaternion.Euler(0f, 0f, startZ);
    }
}
