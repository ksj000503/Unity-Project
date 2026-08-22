using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private WeaponData data;

    [SerializeField] private LayerMask enemyMask;

    private IWeaponBehavior behavior;

    private float cooldownTimer;

    private bool isAttacking;

    // 무기 레벨(런타임 인스턴스 값). SO(WeaponData)는 공유 에셋이라 오염 금지 → 여기서만 관리.
    private int level = 1;

    public WeaponData Data => data;
    public LayerMask EnemyMask => enemyMask;
    public int Level => level;

    // 레벨 반영 최종 수치. 원본 data 값은 기준값으로만 사용하고 절대 수정하지 않는다.
    // 데미지: 레벨당 +20%(가산 배수). Lv1=기준, Lv2=+20%, Lv3=+40% ...
    public int Damage
    {
        get
        {
            if (data == null) return 0;
            float scaled = data.damage * (1f + 0.2f * (level - 1));
            return Mathf.RoundToInt(scaled);
        }
    }

    // 관통: 3레벨마다 +1. Lv1~3=기준, Lv4~6=+1 ...
    public int PierceCount
    {
        get
        {
            if (data == null) return 0;
            int bonus = (level - 1) / 3;
            return Mathf.Max(0, data.pierceCount + bonus);
        }
    }

    public void Setup(WeaponData weaponData, LayerMask mask)
    {
        data = weaponData;

        enemyMask = mask;

        enabled = true;

        InitBehavior();
    }

    // 같은 무기를 다시 획득했을 때 슬롯을 늘리지 않고 강화. 수치는 매 공격 시 계산 프로퍼티로 즉시 반영됨.
    public void LevelUp()
    {
        level++;

        Debug.Log($"[Weapon] {(data != null ? data.name : name)} 레벨업 → Lv{level} (dmg {Damage}, pierce {PierceCount})", this);
    }

    private void Awake()
    {
        if (data != null) InitBehavior();
    }

    private void InitBehavior()
    {
        if (data == null)
        {
            Debug.LogError($"[Weapon] {name}: WeaponData 미할당.", this);

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
