using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField]
    private int maxHp;

    private int currentHp;

    private GameObject sourcePrefab;

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
        else
        {
            Destroy(gameObject);
        }
    }
}
