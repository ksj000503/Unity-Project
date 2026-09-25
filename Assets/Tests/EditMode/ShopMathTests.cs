using NUnit.Framework;

// ── 상점 등급 추첨 테스트 (자동화 + 확률 검증) ─────────────────────────────
// 기획 가중치: Normal 70 / Epic 25 + 행운×2 / Unique 5 + 행운
// 가중치 공식, 굴림값 경계, 실제 분포가 기획 확률과 맞는지를 자동으로 확인한다.

public class ShopMathTests
{
    // [1] 가중치 공식 ─ 예시로 먼저 작성
    [TestCase(0, 70f, 25f, 5f)]
    [TestCase(10, 70f, 45f, 15f)]
    public void 행운에_따른_등급_가중치(int luck, float normal, float epic, float unique)
    {
        float[] w = ShopMath.RarityWeights(luck, true, true, true);

        Assert.AreEqual(normal, w[0], 0.001f, "Normal");
        Assert.AreEqual(epic, w[1], 0.001f, "Epic");
        Assert.AreEqual(unique, w[2], 0.001f, "Unique");
    }

    [Test]
    public void 풀에_없는_등급은_가중치_0()
    {
        float[] w = ShopMath.RarityWeights(10, true, true, false);

        Assert.AreEqual(0f, w[2], 0.001f, "Unique 가 풀에 없으면 0");
    }

    // [2] 굴림값 경계 ─ TODO(성재)
    // 행운 0이면 가중치 70 / 25 / 5, 합계 100.
    // 굴림값 0.70 에서 Normal→Epic, 0.95 에서 Epic→Unique 로 바뀐다.
    // 경계 바로 아래·위 값을 TestCase 로 추가해 보세요. (0, 0.699, 0.70, 0.949, 0.95, 1.0)
    [TestCase(0f, ItemRarity.Normal)]      // 맨 처음

    [TestCase(0.699f, ItemRarity.Normal)]  // Normal 구간 끝

    [TestCase(0.70f, ItemRarity.Epic)]     // Epic 시작

    [TestCase(0.949f, ItemRarity.Epic)]    // Epic 구간 끝

    [TestCase(0.95f, ItemRarity.Unique)]   // Unique 시작

    [TestCase(1f, ItemRarity.Unique)]      // 맨 끝 (Random.value 는 1.0 도 나옴)

    public void 굴림값_경계(float roll, ItemRarity expected)
    {
        float[] w = ShopMath.RarityWeights(0, true, true, true);

        Assert.AreEqual(expected, ShopMath.PickRarity(w, roll));
    }

    // [3] 풀에 없는 등급이 나오는지 ─ TODO(성재)
    // Unity 의 Random.value 는 0과 1을 모두 포함한다(공식 문서).
    // Unique 가 풀에 없을 때 굴림값이 1.0 이면 무엇이 나와야 할까요?
    // 기대값을 먼저 정하고 아래 테스트를 완성한 뒤 실행해 보세요.
    [Test]
    public void 풀에_없는_등급은_뽑히지_않는다()
    {
        // 상점 풀에 Unique 아이템이 하나도 없는 상황 (마지막 인자 false)
        float[] w = ShopMath.RarityWeights(0, true, true, false);

        // 굴림값이 정확히 1.0 이 나온 경우
        ItemRarity result = ShopMath.PickRarity(w, 1f);

        // Unique 는 풀에 없으니, 가장 높은 구간인 Epic 이 나와야 한다
        Assert.AreEqual(ItemRarity.Epic, result, "풀에 없는 Unique 가 뽑힘");
    }

    // [4] 10만 회 시뮬레이션 ─ 실제 분포가 기획 확률과 맞는지
    private const int Trials = 100000;

    [TestCase(0)]
    [TestCase(5)]
    [TestCase(10)]
    [TestCase(20)]
    public void 시뮬레이션_분포가_기획_확률과_일치(int luck)
    {
        float[] w = ShopMath.RarityWeights(luck, true, true, true);
        float total = w[0] + w[1] + w[2];

        // 시드 고정: 매번 같은 난수가 나와 결과가 들쭉날쭉하지 않다
        var rng = new System.Random(12345);
        int[] counts = new int[3];

        for (int i = 0; i < Trials; i++)
        {
            float roll = (float)rng.NextDouble();
            counts[(int)ShopMath.PickRarity(w, roll)]++;
        }

        for (int k = 0; k < 3; k++)
        {
            double expected = w[k] / total;
            double actual = (double)counts[k] / Trials;

            // 허용 오차 = 표준오차 × 4 (정상 코드가 우연히 벗어날 확률 약 0.006%)
            double tolerance = 4 * System.Math.Sqrt(expected * (1 - expected) / Trials);

            TestContext.WriteLine(
                $"행운 {luck} {(ItemRarity)k}: 기대 {expected:P2} / 실제 {actual:P2} (허용 ±{tolerance:P2})");

            Assert.AreEqual(expected, actual, tolerance, $"{(ItemRarity)k} 확률이 기획과 다름");
        }
    }
}
