using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField]
    private int maxHp;

    private int currentHp;

    private GameObject sourcePrefab;

    // 체력이 바뀔 때만 발행(current, max). HealthBar 등 UI가 구독. 값 변경 시에만 갱신 → 폴링 불필요.
    public event System.Action<int, int> OnHealthChanged;

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
