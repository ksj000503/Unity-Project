using System.Collections.Generic;
using UnityEngine;

// 플레이어 강화 스탯 컨테이너. 아이템 구매로 값이 오르고, 무기/체력이 이 값을 읽어 반영한다.
// 또한 장착 무기 구성(근접/원거리 개수)에 따른 세트 보너스를 계산해 무기 타입별 데미지 배수로 제공한다.
// 상점 스탯 패널(현재 스탯)의 소스이기도 함(GetStatLines).
[RequireComponent(typeof(Health))]
public class PlayerStats : MonoBehaviour
{
    [Header("아이템 강화치 (아이템 구매로 증가)")]
    [Tooltip("무기 데미지 가산 %(예: 20 = +20%)")]
    [SerializeField] private float damageBonusPercent = 0f;

    [Tooltip("공격 쿨다운 감소 %(예: 15 = -15%)")]
    [SerializeField] private float cooldownReductionPercent = 0f;

    [Tooltip("행운: 상점에서 상위 등급 아이템이 잘 나오게 함")]
    [SerializeField] private int luck = 0;

    [Tooltip("최대 HP(표시용 캐시). 실제 HP는 Health 컴포넌트가 관리")]
    [SerializeField] private int maxHp = 100;

    [Header("세트 효과")]
    [Tooltip("같은 타입 무기 2/4/6개마다 1단계씩. 단계당 그 타입 무기 데미지 +이 값(%)")]
    [SerializeField] private float setDamagePerTier = 10f;

    // 스탯이 바뀌면 발행(상점 스탯 패널 갱신용).
    public event System.Action OnChanged;

    private Health health;
    private WeaponSlotManager slotManager;

    // 세트 계산 결과(표시/배수용).
    private int meleeCount;
    private int rangedCount;
    private float meleeSetPercent;
    private float rangedSetPercent;

    // 무기 데미지 배수. Weapon.Damage 가 곱해 사용.
    public float DamageMultiplier => 1f + damageBonusPercent / 100f;

    // 타입별 세트 배수. 근접/원거리 무기가 각자 자기 타입 것만 곱한다.
    public float MeleeSetMultiplier => 1f + meleeSetPercent / 100f;
    public float RangedSetMultiplier => 1f + rangedSetPercent / 100f;

    // 공격 쿨다운 배수. 과도 감소로 0 이 되지 않게 하한 0.2(=최대 80% 감소).
    public float CooldownMultiplier => Mathf.Clamp(1f - cooldownReductionPercent / 100f, 0.2f, 1f);

    public int Luck => luck;
    public int MaxHp => maxHp;

    private void Awake()
    {
        health = GetComponent<Health>();

        if (health != null) maxHp = health.MaxHp(); // 실제 최대 HP 와 동기화
    }

    private void Start()
    {
        slotManager = GetComponent<WeaponSlotManager>();

        if (slotManager == null) slotManager = GetComponentInChildren<WeaponSlotManager>();

        if (slotManager == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null) slotManager = player.GetComponentInChildren<WeaponSlotManager>();
        }

        if (slotManager != null) slotManager.OnWeaponsChanged += RecomputeSet;

        RecomputeSet(); // 시작 무기 포함 초기 계산
    }

    private void OnDestroy()
    {
        if (slotManager != null) slotManager.OnWeaponsChanged -= RecomputeSet;
    }

    // ---------- 세트 효과 ----------

    // 장착 무기를 타입별로 세어 세트 보너스를 다시 계산. 무기 구성이 바뀔 때마다 호출됨.
    private void RecomputeSet()
    {
        meleeCount = 0;
        rangedCount = 0;

        if (slotManager != null)
        {
            for (int i = 0; i < slotManager.SlotCount; i++)
            {
                Weapon w = slotManager.GetSlot(i);

                if (w == null || w.Data == null) continue;

                if (w.Data.weaponType == WeaponType.RangedShoot) rangedCount++;
                else meleeCount++;
            }
        }

        meleeSetPercent = SetTiers(meleeCount) * setDamagePerTier;
        rangedSetPercent = SetTiers(rangedCount) * setDamagePerTier;

        OnChanged?.Invoke();
    }

    // 문턱 2/4/6 을 넘은 단계 수(0~3).
    private static int SetTiers(int count)
    {
        if (count >= 6) return 3;
        if (count >= 4) return 2;
        if (count >= 2) return 1;
        return 0;
    }

    // ---------- 아이템 효과 적용 지점 ----------

    public void AddDamagePercent(float percent)
    {
        damageBonusPercent += percent;

        OnChanged?.Invoke();
    }

    public void AddCooldownReduction(float percent)
    {
        cooldownReductionPercent += percent;

        OnChanged?.Invoke();
    }

    public void AddLuck(int amount)
    {
        luck = Mathf.Max(0, luck + amount);

        OnChanged?.Invoke();
    }

    // 최대 HP 증가 + 증가분만큼 현재 HP 회복(구매 보상감).
    public void AddMaxHp(int amount)
    {
        maxHp = Mathf.Max(1, maxHp + amount);

        if (health != null) health.AddMax(amount);

        OnChanged?.Invoke();
    }

    // 표시용 "이름  값" 라인 목록. 상점 스탯 패널이 그대로 출력.
    public List<string> GetStatLines()
    {
        var lines = new List<string>
        {
            $"데미지      +{damageBonusPercent:0.#}%",
            $"공격 쿨감   {cooldownReductionPercent:0.#}%",
            $"최대 HP     {maxHp}",
            $"행운        {luck}",
        };

        if (meleeSetPercent > 0f)
            lines.Add($"근접 세트   +{meleeSetPercent:0.#}% ({meleeCount})");

        if (rangedSetPercent > 0f)
            lines.Add($"원거리 세트 +{rangedSetPercent:0.#}% ({rangedCount})");

        return lines;
    }
}
