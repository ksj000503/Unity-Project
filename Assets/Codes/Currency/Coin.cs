using UnityEngine;

// 몬스터가 떨군 돈. 자석형 획득: 플레이어가 attractRange 안에 들어오면 끌려가고,
// collectRange 안이면 획득 → 지갑에 값 적립 후 풀 복귀. ObjectPoolManager 로 풀링.
public class Coin : MonoBehaviour
{
    [Header("자석 범위 (월드 단위)")]
    [Tooltip("이 거리 안이면 플레이어 쪽으로 끌려가기 시작")]
    [SerializeField] private float attractRange = 2.5f;

    [Tooltip("이 거리 안이면 획득")]
    [SerializeField] private float collectRange = 0.4f;

    [Tooltip("끌려가는 속도(월드 유닛/초)")]
    [SerializeField] private float moveSpeed = 8f;

    private int value = 1;
    private GameObject sourcePrefab;

    private bool isCollected;
    private Transform playerTf;
    private CurrencyWallet wallet;

    // CoinDropper 가 스폰 직후 호출. sourcePrefab 은 풀 반납 키, value 는 이 코인의 가치.
    public void Setup(GameObject sourcePrefab, int value)
    {
        this.sourcePrefab = sourcePrefab;

        this.value = Mathf.Max(1, value);
    }

    private void OnEnable()
    {
        isCollected = false;

        CachePlayer();
    }

    private void CachePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            playerTf = null;
            wallet = null;
            return;
        }

        playerTf = player.transform;
        wallet = player.GetComponent<CurrencyWallet>();
    }

    private void FixedUpdate()
    {
        if (isCollected) return;

        // 플레이어가 아직 없으면(스폰 순서 등) 재탐색 시도.
        if (playerTf == null)
        {
            CachePlayer();

            if (playerTf == null) return;
        }

        float sqr = ((Vector2)playerTf.position - (Vector2)transform.position).sqrMagnitude;

        if (sqr <= collectRange * collectRange)
        {
            Collect();

            return;
        }

        if (sqr <= attractRange * attractRange)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                playerTf.position,
                moveSpeed * Time.fixedDeltaTime);
        }
    }

    private void Collect()
    {
        if (isCollected) return;

        isCollected = true;

        if (wallet != null)
        {
            wallet.Add(value);
        }
        else
        {
            Debug.LogWarning("[Coin] 플레이어에 CurrencyWallet 없음 — 값 저장 실패, 코인만 회수.", this);
        }

        if (sourcePrefab != null)
        {
            ObjectPoolManager.Instance.Return(sourcePrefab, gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
