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

        Vector2 baseDir = ((Vector2)target.position - (Vector2)tf.position).normalized;

        int count = Mathf.Max(1, data.projectileCount);

        float spread = Mathf.Max(0f, data.spreadAngle);

        // 다발이면 -spread/2 ~ +spread/2 를 count-1 등분해 부채꼴로 발사. 단발이면 그대로 일직선.
        for (int i = 0; i < count; i++)
        {
            float angle = 0f;

            if (count > 1 && spread > 0f)
            {
                float t = (float)i / (count - 1); // 0..1

                angle = Mathf.Lerp(-spread * 0.5f, spread * 0.5f, t);
            }

            Vector2 dir = Rotate(baseDir, angle);

            GameObject projGO = ObjectPoolManager.Instance.Get(data.projectilePrefab);

            projGO.transform.position = tf.position;

            Projectile projectile = projGO.GetComponent<Projectile>();

            if (projectile == null)
            {
                Debug.LogError($"[RangedShootBehavior] {data.name}.projectilePrefab 에 Projectile 컴포넌트 없음.");

                yield break;
            }

            projectile.Launch(dir, owner.Damage, data.projectilePrefab, owner.EnemyMask, owner.PierceCount);
        }
    }

    // 2D 벡터를 도(degree) 단위로 회전.
    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;

        float cos = Mathf.Cos(rad);

        float sin = Mathf.Sin(rad);

        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}
