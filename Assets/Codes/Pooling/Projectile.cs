using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField]
    private float speed;
    [SerializeField]
    private float lifeTime;

    private int damage;

    private Vector2 direction;

    private float timer;

    private GameObject sourcePrefab;

    private bool isReturned;

    private bool useTargetMask;
    private LayerMask targetMask;

    private int pierceRemaining;

    private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    // 몬스터용(기존): 대상 필터 없음, 단일 타격
    public void Launch(Vector2 dir, int dmg, GameObject prefab)
    {
        InitCommon(dir, dmg, prefab);

        useTargetMask = false;

        pierceRemaining = 0;
    }

    // 플레이어 무기용: 타겟 레이어 필터 + 관통 횟수(pierceCount = 추가 관통 수, 0이면 단일)
    public void Launch(Vector2 dir, int dmg, GameObject prefab, LayerMask mask, int pierceCount)
    {
        InitCommon(dir, dmg, prefab);

        useTargetMask = true;

        targetMask = mask;

        pierceRemaining = Mathf.Max(0, pierceCount);
    }

    private void InitCommon(Vector2 dir, int dmg, GameObject prefab)
    {
        direction = dir;

        damage = dmg;

        sourcePrefab = prefab;

        timer = 0f;

        isReturned = false;

        hitTargets.Clear();
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        timer += Time.deltaTime;

        if (timer >= lifeTime)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (useTargetMask)
        {
            // 플레이어 무기 경로: 지정 레이어의 IDamageable만 처리(그 외 충돌은 통과)
            if ((targetMask.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            IDamageable damageable = other.GetComponent<IDamageable>();

            if (damageable == null)
            {
                return;
            }
            if (hitTargets.Contains(damageable))
            {
                return;
            }

            hitTargets.Add(damageable);

            damageable.TakeDamage(damage);

            if (pierceRemaining > 0)
            {
                pierceRemaining--;

                return;
            }

            ReturnToPool();
        }
        else
        {
            IDamageable damageable = other.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (isReturned) return;

        isReturned = true;

        ObjectPoolManager.Instance.Return(sourcePrefab, gameObject);
    }
}