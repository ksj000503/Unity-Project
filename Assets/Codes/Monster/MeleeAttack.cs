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
        IDamageable damageable = other.GetComponent<IDamageable>();

        if(damageable == null)
        {
            return;
        }

        damageable.TakeDamage(damage);
    }
}
