using UnityEngine;

// 플레이어 재화(런 골드) 저장소. 인메모리 누적 + 변경 이벤트(추후 UI 연결).
// 플레이어 오브젝트에 부착. 코인 획득 시 Add 호출.
public class CurrencyWallet : MonoBehaviour
{
    [SerializeField] private int coins = 0;

    public int Coins => coins;

    // 잔액이 바뀔 때 발행(새 잔액). 재화 UI 등이 구독.
    public event System.Action<int> OnChanged;

    public void Add(int amount)
    {
        if (amount <= 0) return;

        coins += amount;

        OnChanged?.Invoke(coins);
    }

    // 소비(상점 등). 잔액 부족 시 false.
    public bool TrySpend(int amount)
    {
        if (amount <= 0 || coins < amount) return false;

        coins -= amount;

        OnChanged?.Invoke(coins);

        return true;
    }
}
