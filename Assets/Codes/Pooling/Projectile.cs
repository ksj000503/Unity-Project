using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField]
    private float speed;

    [SerializeField]
    private float lifeTime;

    private int damage;

    private Vector2 direction;

    private float timer;

    private GameObject sourcePrefab;

    private bool isReturned;

    public void Launch(Vector2 dir, int dmg, GameObject prefab)
    {
        direction = dir;

        damage = dmg;

        sourcePrefab = prefab;

        timer = 0f;

        isReturned = false;
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        timer += Time.deltaTime;

        if(timer >= lifeTime)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (isReturned) return;
        isReturned = true;

        ObjectPoolManager.Instance.Return(sourcePrefab, gameObject);
    }
}
