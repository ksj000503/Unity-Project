using UnityEngine;

// 무기 수치 계산을 담는 순수 함수 모음(MonoBehaviour 아님).
// Weapon 컴포넌트에서 떼어내 자동 테스트로 검증할 수 있게 분리. 동작은 기존과 동일.
public static class WeaponMath
{
    // 레벨 반영 데미지. 레벨당 +20%(가산) × 아이템 배수 × 세트 배수 후 반올림.
    // Lv1=기준, Lv2=+20% ... (예: 기본5 → 5,6,7,8,9)
    public static int FinalDamage(int baseDamage, int level, float itemMultiplier, float setMultiplier)
    {
        float scaled = baseDamage * (1f + 0.2f * (level - 1));

        return Mathf.RoundToInt(scaled * itemMultiplier * setMultiplier);
    }

    // 배수 없는 순수 레벨 데미지(표시/검증용). = FinalDamage(base, level, 1, 1)
    public static int ScaledDamage(int baseDamage, int level)
    {
        return FinalDamage(baseDamage, level, 1f, 1f);
    }

    // 관통 수: 3레벨마다 +1. Lv1~3=기준, Lv4~6=+1 ...
    public static int PierceCount(int basePierce, int level)
    {
        int bonus = (level - 1) / 3;

        return Mathf.Max(0, basePierce + bonus);
    }
}
