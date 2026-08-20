using System.Collections;
using UnityEngine;

public class RangedShootBehavior : IWeaponBehavior
{
    private Weapon owner;

    private WeaponData data;

    private Transform tf;

    public void Initialize(Weapon owner, WeaponData data)
    {
        this.owner = owner;

        this.data = data;

        this.tf = owner.transform;
    }

    public IEnumerator Execute(Transform target)
    {
        if (target == null)
        {
            yield break;
        }

        if (data.projectilePrefab == null)
        {
            Debug.LogError($"[RangedShootBehavior] {data.name}: projectilePrefab 미할당.");

            yield break;
        }

        Vector2 dir = ((Vector2)target.position - (Vector2)tf.position).normalized;

        GameObject projGO = ObjectPoolManager.Instance.Get(data.projectilePrefab);

        projGO.transform.position = tf.position;

        Projectile projectile = projGO.GetComponent<Projectile>();

        if (projectile == null)
        {
            Debug.LogError($"[RangedShootBehavior] {data.name}.projectilePrefab 에 Projectile 컴포넌트 없음.");

            yield break;
        }

        projectile.Launch(dir, data.damage, data.projectilePrefab, owner.EnemyMask, data.pierceCount);
    }
}