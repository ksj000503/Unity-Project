using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField]
    private int maxHp;

    private int currentHp;

    private GameObject sourcePrefab;

    void OnEnable()
    {
        currentHp = maxHp;    
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

        if(currentHp <= 0) 
        {
            Die();
        }
    }

    void Die()
    {
        if(sourcePrefab != null)
        {
            ObjectPoolManager.Instance.Return(sourcePrefab, gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
