using NUnit.Framework;
using UnityEngine;

// ── 첫 자동 테스트 (EditMode) ─────────────────────────────────────────────
// EditMode 테스트는 게임을 실행하지 않고 코드만 돌려서 검증한다 → 빠르고 안정적.
// [Test] 가 붙은 메서드 하나 = 테스트 케이스 하나. Assert 가 하나라도 틀리면 FAIL.
// Unity 상단 메뉴 Window > General > Test Runner 에서 실행/결과 확인.

public class SetBonusTests
{
    // 세트 단계: 무기 개수 2/4/6 문턱을 넘을 때마다 1단계씩(최대 3단계).
    // 0~1개=0, 2~3개=1, 4~5개=2, 6개 이상=3 이어야 한다.
    [Test]
    public void SetTiers_문턱마다_1단계씩_오른다()
    {
        Assert.AreEqual(0, PlayerStats.SetTiers(0), "0개는 0단계");
        Assert.AreEqual(0, PlayerStats.SetTiers(1), "1개는 아직 0단계");
        Assert.AreEqual(1, PlayerStats.SetTiers(2), "2개부터 1단계");
        Assert.AreEqual(1, PlayerStats.SetTiers(3));
        Assert.AreEqual(2, PlayerStats.SetTiers(4), "4개부터 2단계");
        Assert.AreEqual(2, PlayerStats.SetTiers(5));
        Assert.AreEqual(3, PlayerStats.SetTiers(6), "6개부터 3단계");
        Assert.AreEqual(3, PlayerStats.SetTiers(7), "6개 넘어도 3단계로 고정");
    }

    // 음수/비정상 입력도 0단계로 안전하게 처리되는지(방어).
    [Test]
    public void SetTiers_음수는_0단계()
    {
        Assert.AreEqual(0, PlayerStats.SetTiers(-1));
        Assert.AreEqual(0, PlayerStats.SetTiers(-100));
    }

    // [TestCase] 로 여러 입력을 한 메서드에서 표처럼 검증할 수 있다.
    // 단계당 +10% 라면: 2개=+10, 4개=+20, 6개=+30 이어야 한다.
    [TestCase(0, 0f)]
    [TestCase(2, 10f)]
    [TestCase(4, 20f)]
    [TestCase(6, 30f)]
    public void 세트_데미지퍼센트_단계에_비례한다(int weaponCount, float expectedPercent)
    {
        float perTier = 10f;

        float actual = PlayerStats.SetTiers(weaponCount) * perTier;

        Assert.AreEqual(expectedPercent, actual);
    }

    // ScriptableObject(WeaponData)의 기본 최대 레벨이 5인지 확인.
    // new 로 못 만들고 ScriptableObject.CreateInstance 로 생성하는 게 포인트.
    [Test]
    public void WeaponData_기본_최대레벨은_5()
    {
        WeaponData data = ScriptableObject.CreateInstance<WeaponData>();

        Assert.AreEqual(5, data.maxLevel);

        Object.DestroyImmediate(data); // 테스트 뒷정리
    }
}
