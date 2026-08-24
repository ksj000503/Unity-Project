using UnityEngine;

// 몬스터 사망 시 그 자리에 코인을 떨군다. Health.OnDied 를 구독.
// 코인 가치 = baseCoinValue × 현재 스테이지 (StageManager 없으면 스테이지 1).
[RequireComponent(typeof(Health))]
public class CoinDropper : MonoBehaviour
{
    [Header("드랍 설정")]
    [Tooltip("풀링되는 코인 프리팹(Coin 컴포넌트 필요)")]
    [SerializeField] private GameObject coinPrefab;

    [Tooltip("스테이지 1 기준 코인 1개 가치")]
    [SerializeField] private int baseCoinValue = 1;

    [Tooltip("한 번에 떨구는 코인 개수")]
    [SerializeField] private int dropCount = 1;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (health != null) health.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (health != null) health.OnDied -= HandleDied;
    }

    private void HandleDied()
    {
        if (coinPrefab == null)
        {
            Debug.LogWarning($"[CoinDropper] {name}: coinPrefab 미할당 — 드랍 스킵.", this);

            return;
        }

        int stage = (StageManager.Instance != null) ? StageManager.Instance.CurrentStage : 1;

        int value = Mathf.Max(1, baseCoinValue) * stage;

        int count = Mathf.Max(1, dropCount);

        Vector2 origin = transform.position;

        for (int i = 0; i < count; i++)
        {
            // 여러 개면 살짝 흩뿌림.
            Vector2 pos = (count > 1) ? origin + Random.insideUnitCircle * 0.3f : origin;

            GameObject go = ObjectPoolManager.Instance.Get(coinPrefab);

            go.transform.position = pos;

            Coin coin = go.GetComponent<Coin>();

            if (coin == null)
            {
                Debug.LogError($"[CoinDropper] {coinPrefab.name} 에 Coin 컴포넌트 없음.", this);

                ObjectPoolManager.Instance.Return(coinPrefab, go);

                return;
            }

            coin.Setup(coinPrefab, value);
        }
    }
}
