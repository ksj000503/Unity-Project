// 상점 등급 추첨 계산을 담는 순수 함수 모음(MonoBehaviour 아님).
// ShopManager.RollItem 에서 떼어내 자동 테스트로 검증할 수 있게 분리. 동작은 기존과 동일.
public static class ShopMath
{
    public const float NormalBase = 70f;
    public const float EpicBase = 25f;
    public const float UniqueBase = 5f;
    public const float EpicPerLuck = 2f;
    public const float UniquePerLuck = 1f;

    // 행운과 풀 구성에 따른 등급별 가중치. 인덱스는 (int)ItemRarity 순서.
    // 풀에 없는 등급은 뽑히면 안 되므로 가중치 0.
    public static float[] RarityWeights(int luck, bool hasNormal, bool hasEpic, bool hasUnique)
    {
        return new float[]
        {
            hasNormal ? NormalBase : 0f,
            hasEpic ? EpicBase + luck * EpicPerLuck : 0f,
            hasUnique ? UniqueBase + luck * UniquePerLuck : 0f,
        };
    }

    // roll01 은 0~1 사이 난수. 가중치 구간에 따라 등급을 고른다.
    public static ItemRarity PickRarity(float[] weights, float roll01)
    {
        float total = weights[0] + weights[1] + weights[2];

        if (total <= 0f) return ItemRarity.Normal;

        float r = roll01 * total;
        float acc = 0f;
        int last = 0;

        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i] <= 0f) continue; // 풀에 없는 등급은 건너뜀

            acc += weights[i];
            last = i;

            if (r < acc) return (ItemRarity)i;
        }

        // Random.value 는 1.0 도 나오므로 r == total 이면 어떤 구간에도 안 들어간다.
        // 이때는 가중치가 있는 마지막 등급을 고른다.
        return (ItemRarity)last;
    }
}
