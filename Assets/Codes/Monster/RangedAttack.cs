using UnityEngine;

public class RangedAttack : MonoBehaviour, IMonsterAttack
{
    [SerializeField]
    private float attackRange;

    [SerializeField]
    private int damage;

    private Transform target;

    [SerializeField] 
    private float attackCooldown;

    private float cooldownTimer;

    [SerializeField] 
    private GameObject projectilePrefab;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("[RangedAttack] Player를 찾지 못했습니다");
            return;
        }

        target = player.transform;
    }

    public void TryAttack(Transform player)
    {
        if (cooldownTimer > 0f)
        {
            return;
        }

        float sqrDistance = ((Vector2)player.position - (Vector2)transform.position).sqrMagnitude;

        if (sqrDistance > attackRange * attackRange)
        {
            return;
        }

        FireProjectile(player.position);

        cooldownTimer = attackCooldown;
    }

    private void FireProjectile(Vector2 targetPos)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[RangedAttack] projectilePrefab이 비어있습니다.");

            return;
        }

        GameObject projGO = ObjectPoolManager.Instance.Get(projectilePrefab);

        projGO.transform.position = transform.position;

        Vector2 direction = (targetPos - (Vector2)transform.position).normalized;

        Projectile projectile = projGO.GetComponent<Projectile>();

        if (projectile == null)
        {
            Debug.LogError("[RangedAttack] projectilePrefab에 Projectile 컴포넌트가 없습니다.");

            return;
        }

        projectile.Launch(direction, damage, projectilePrefab);
    }

    void Update()
    {
        if(cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
        TryAttack(target);
    }
}
