using NUnit.Framework;

// ── 무기 수치 테스트 (자동화 + 수치화) ────────────────────────────────────
// "이 입력이면 이 숫자여야 한다" 를 표처럼 박아둔다.
// 밸런싱하다 공식이나 배율을 잘못 건드리면 여기서 FAIL 로 바로 잡힌다.

public class WeaponMathTests
{
    // 레벨별 데미지 명세: 레벨당 +20%(가산). 기본 5 무기 → 5,6,7,8,9
    [TestCase(5, 1, 5)]
    [TestCase(5, 2, 6)]
    [TestCase(5, 3, 7)]
    [TestCase(5, 4, 8)]
    [TestCase(5, 5, 9)]
    [TestCase(12, 1, 12)]
    [TestCase(12, 5, 22)]   // 12 × 1.8 = 21.6 → 22
    public void 레벨별_데미지_공식(int baseDamage, int level, int expected)
    {
        Assert.AreEqual(expected, WeaponMath.ScaledDamage(baseDamage, level));
    }

    // 아이템·세트 배수가 곱으로 반영되는지.
    [Test]
    public void 데미지_배수_적용()
    {
        Assert.AreEqual(15, WeaponMath.FinalDamage(10, 1, 1.5f, 1f), "아이템 +50%");
        Assert.AreEqual(11, WeaponMath.FinalDamage(10, 1, 1f, 1.1f), "세트 1단계 +10%");
        Assert.AreEqual(18, WeaponMath.FinalDamage(10, 1, 1.5f, 1.2f), "아이템×세트 = 1.5×1.2");
    }

    // 관통: 3레벨마다 +1.
    [TestCase(0, 1, 0)]
    [TestCase(0, 3, 0)]
    [TestCase(0, 4, 1)]
    [TestCase(0, 6, 1)]
    [TestCase(0, 7, 2)]
    [TestCase(1, 1, 1)]   // 석궁: 기본 관통 1
    [TestCase(1, 4, 2)]
    public void 관통_3레벨마다_1증가(int basePierce, int level, int expected)
    {
        Assert.AreEqual(expected, WeaponMath.PierceCount(basePierce, level));
    }
}
