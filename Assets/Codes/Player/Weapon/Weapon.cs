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

    // 플레이어 강화 스탯(아이템). 부모(Player)에서 참조. 없으면 배수 1(무보정).
    private PlayerStats stats;

    public WeaponData Data => data;
    public LayerMask EnemyMask => enemyMask;
    public int Level => level;

    // HUD 표시용. 무기 아이콘 스프라이트와 최대 레벨(레벨 칸 수).
    public Sprite Icon => data != null ? data.icon : null;
    public int MaxLevel => data != null ? Mathf.Max(1, data.maxLevel) : 1;

    // 아직 강화 여지가 있는지(상한 미도달). 상점이 중복 구매 가능 여부로 사용.
    public bool CanLevelUp => level < MaxLevel;

    // 레벨 반영 최종 수치. 원본 data 값은 기준값으로만 사용하고 절대 수정하지 않는다.
    // 데미지: 레벨당 +20%(가산 배수) × 아이템 데미지 배수 × 타입별 세트 배수. Lv1=기준, Lv2=+20% ...
    public int Damage
    {
        get
        {
            if (data == null) return 0;
            float scaled = data.damage * (1f + 0.2f * (level - 1));
            float itemMult = (stats != null) ? stats.DamageMultiplier : 1f;
            float setMult = 1f;
            if (stats != null)
                setMult = (data.weaponType == WeaponType.RangedShoot) ? stats.RangedSetMultiplier : stats.MeleeSetMultiplier;
            return Mathf.RoundToInt(scaled * itemMult * setMult);
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

        if (stats == null) stats = GetComponentInParent<PlayerStats>();

        InitBehavior();
    }

    // 같은 무기를 다시 획득했을 때 슬롯을 늘리지 않고 강화. 최대 레벨에서 더 오르지 않음(상한 클램프).
    public void LevelUp()
    {
        level = Mathf.Min(level + 1, MaxLevel);

        Debug.Log($"[Weapon] {(data != null ? data.name : name)} 레벨업 → Lv{level}/{MaxLevel} (dmg {Damage}, pierce {PierceCount})", this);
    }

    private void Awake()
    {
        if (stats == null) stats = GetComponentInParent<PlayerStats>();

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

        // 아이템 쿨감 배수 반영. 하한 0.05 로 0 방지.
        float cdMult = (stats != null) ? stats.CooldownMultiplier : 1f;

        cooldownTimer = Mathf.Max(0.05f, data.attackCooldown * cdMult);

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
