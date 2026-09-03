using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField]
    private int maxHp;

    private int currentHp;

    private GameObject sourcePrefab;

    [Tooltip("사망 시 오브젝트 파괴 여부. 플레이어는 false(파괴 대신 OnDied만 발행 → 게임오버 처리).")]
    [SerializeField] private bool destroyOnDeath = true;

    // 체력이 바뀔 때만 발행(current, max). HealthBar 등 UI가 구독. 값 변경 시에만 갱신 → 폴링 불필요.
    public event System.Action<int, int> OnHealthChanged;

    // 사망 순간 1회 발행(풀 복귀/파괴 직전). 인자는 죽은 본인(Health) — 스포너가 어떤 몬스터인지 식별.
    public event System.Action<Health> OnDied;

    void OnEnable()
    {
        currentHp = maxHp;

        // 스폰/풀 재사용 시 풀피로 초기화된 상태를 통지(구독자가 있으면).
        OnHealthChanged?.Invoke(currentHp, maxHp);
    }

    public void SetSourcePrefab(GameObject prefab)
    {
        sourcePrefab = prefab;
    }

    // 최대 HP 재설정(스테이지 스케일 등). 현재 HP도 풀피로 맞추고 통지. 스폰 직후 호출 상정.
    public void SetMax(int value)
    {
        maxHp = Mathf.Max(1, value);

        currentHp = maxHp;

        OnHealthChanged?.Invoke(currentHp, maxHp);
    }

    // 최대 HP 를 delta 만큼 늘리고 현재 HP 도 같이 회복(아이템 HP+ 구매용). 풀피로 리셋하지 않음.
    public void AddMax(int delta)
    {
        maxHp = Mathf.Max(1, maxHp + delta);

        currentHp = Mathf.Min(maxHp, currentHp + Mathf.Max(0, delta));

        OnHealthChanged?.Invoke(currentHp, maxHp);
    }

    public int MaxHp()
    {
        return maxHp;
    }

    public int CurrentHp()
    {
        return currentHp;
    }

    public void TakeDamage(int amount)
    {
        if (currentHp <= 0)
        {
            return;
        }

        currentHp -= amount;

        OnHealthChanged?.Invoke(currentHp, maxHp);

        if (currentHp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        OnDied?.Invoke(this);

        if (sourcePrefab != null)
        {
            ObjectPoolManager.Instance.Return(sourcePrefab, gameObject);
        }
        else if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
        // destroyOnDeath=false 이고 풀 소속도 아니면(플레이어): 파괴 안 하고 OnDied 로만 처리.
    }
}
