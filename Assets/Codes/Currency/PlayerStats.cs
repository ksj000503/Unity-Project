using System.Collections.Generic;
using UnityEngine;

// 플레이어 강화 스탯 컨테이너. 아이템 구매로 값이 오르고, 무기/체력이 이 값을 읽어 반영한다.
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

    // 스탯이 바뀌면 발행(상점 스탯 패널 갱신용).
    public event System.Action OnChanged;

    private Health health;

    // 무기 데미지 배수. Weapon.Damage 가 곱해 사용.
    public float DamageMultiplier => 1f + damageBonusPercent / 100f;

    // 공격 쿨다운 배수. 과도 감소로 0 이 되지 않게 하한 0.2(=최대 80% 감소).
    public float CooldownMultiplier => Mathf.Clamp(1f - cooldownReductionPercent / 100f, 0.2f, 1f);

    public int Luck => luck;
    public int MaxHp => maxHp;

    private void Awake()
    {
        health = GetComponent<Health>();

        if (health != null) maxHp = health.MaxHp(); // 실제 최대 HP 와 동기화
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
        return new List<string>
        {
            $"데미지      +{damageBonusPercent:0.#}%",
            $"공격 쿨감   {cooldownReductionPercent:0.#}%",
            $"최대 HP     {maxHp}",
            $"행운        {luck}",
        };
    }
}
