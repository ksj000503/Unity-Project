using System.Collections.Generic;
using UnityEngine;

// 플레이어 스탯 표시용 컨테이너(현재 스탯 패널 소스). 지금은 값 보관·표시만 담당.
// 이후 장비/버프 시스템이 이 값들을 조정하면 상점 스탯 패널에 그대로 반영됨.
public class PlayerStats : MonoBehaviour
{
    [SerializeField] private float attackPower = 1f;
    [SerializeField] private float attackSpeed = 1f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private int maxHp = 100;

    // 스탯이 바뀌면 발행(상점 스탯 패널 갱신용).
    public event System.Action OnChanged;

    public float AttackPower => attackPower;
    public float AttackSpeed => attackSpeed;
    public float MoveSpeed => moveSpeed;
    public int MaxHp => maxHp;

    // 표시용 "이름  값" 라인 목록. 항목 추가는 여기만 늘리면 상점 패널이 자동 반영.
    public List<string> GetStatLines()
    {
        return new List<string>
        {
            $"공격력      {attackPower:0.##}",
            $"공격속도    {attackSpeed:0.##}",
            $"이동속도    {moveSpeed:0.##}",
            $"최대 HP     {maxHp}",
        };
    }

    // 장비/버프 등에서 스탯을 올릴 때 호출(확장 지점).
    public void AddAttackPower(float amount)
    {
        attackPower = Mathf.Max(0f, attackPower + amount);

        OnChanged?.Invoke();
    }

    public void AddAttackSpeed(float amount)
    {
        attackSpeed = Mathf.Max(0f, attackSpeed + amount);

        OnChanged?.Invoke();
    }
}
