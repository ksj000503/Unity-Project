using UnityEngine;

public class MeleeAttack : MonoBehaviour, IMonsterAttack
{
    [SerializeField]
    private int damage;

    public void TryAttack(Transform player)
    {

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 몬스터 근접 공격은 플레이어만 타격(몬스터끼리 서로 때리는 피아 피해 방지).
        if (!other.CompareTag("Player"))
        {
            return;
        }

        IDamageable damageable = other.GetComponent<IDamageable>();

        if (damageable == null)
        {
            return;
        }

        damageable.TakeDamage(damage);
    }
}
