using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private WeaponData data;

    [SerializeField] private LayerMask enemyMask;

    private IWeaponBehavior behavior;

    private float cooldownTimer;

    private bool isAttacking;

    public WeaponData Data => data;
    public LayerMask EnemyMask => enemyMask;

    public void Setup(WeaponData weaponData, LayerMask mask)
    {
        data = weaponData;

        enemyMask = mask;

        enabled = true;

        InitBehavior();
    }

    private void Awake()
    {
        if (data != null) InitBehavior();
    }

    private void InitBehavior()
    {
        if (data == null)
        {
            Debug.LogError($"[Weapon] {name}: WeaponData ¹ÌÇÒ´ç.", this);

            enabled = false;

            return;
        }


        switch (data.weaponType)
        {
            case WeaponType.RangedShoot:
                behavior = new RangedShootBehavior();
                break;
            default:
                behavior = new MeleeSpinBehavior();
                break;
        }
        behavior.Initialize(this, data);
    }

    private void Update()
    {
        if (behavior == null)
        {
            return;
        }

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (isAttacking || cooldownTimer > 0f)
        {
            return;
        }

        Transform target = FindNearestEnemy();

        if (target != null)
        {
            StartCoroutine(AttackRoutine(target));
        }
            
    }

    private IEnumerator AttackRoutine(Transform target)
    {
        isAttacking = true;

        yield return behavior.Execute(target);

        cooldownTimer = Mathf.Max(0.01f, data.attackCooldown);

        isAttacking = false;
    }
    private Transform FindNearestEnemy()
    {
        float range = Mathf.Max(0.01f, data.detectRange);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, enemyMask);

        Transform nearest = null;

        float bestSqr = float.MaxValue;

        Vector2 origin = transform.position;

        foreach (var h in hits)
        {
            if (h == null)
            {
                continue;
            }
            float sqr = ((Vector2)h.transform.position - origin).sqrMagnitude;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                nearest = h.transform;
            }
        }
        return nearest;
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(transform.position, data.detectRange);

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(transform.position, data.spinRadius);
    }
}