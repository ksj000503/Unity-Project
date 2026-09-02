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

    [Tooltip("투사체 생성 위치를 발사 방향으로 밀어내는 거리(자기 콜라이더와 겹쳐 자기피격되는 것 방지)")]
    [SerializeField]
    private float spawnOffset = 0.7f;

    // 플레이어 레이어만 맞히는 마스크(Start 에서 자동 설정). 무마스크 발사와 달리
    // 자기 자신·다른 몬스터를 맞히지 않고 오직 플레이어에게만 피해 → 아군/자기피격 차단.
    private LayerMask playerMask;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("[RangedAttack] Player�� ã�� ���߽��ϴ�");
            return;
        }

        target = player.transform;

        playerMask = 1 << player.layer;
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
            Debug.LogWarning("[RangedAttack] projectilePrefab�� ����ֽ��ϴ�.");

            return;
        }

        Vector2 direction = (targetPos - (Vector2)transform.position).normalized;

        GameObject projGO = ObjectPoolManager.Instance.Get(projectilePrefab);

        if (projGO == null) return;

        // 발사 방향으로 살짝 앞에서 생성 → 자기 콜라이더와의 즉시 겹침 방지.
        projGO.transform.position = transform.position + (Vector3)(direction * spawnOffset);

        Projectile projectile = projGO.GetComponent<Projectile>();

        if (projectile == null)
        {
            Debug.LogError("[RangedAttack] projectilePrefab�� Projectile ������Ʈ�� �����ϴ�.");

            return;
        }

        // 플레이어 레이어만 타격하는 마스크 발사(관통 0). 자기 자신·다른 몬스터엔 피해 없음.
        projectile.Launch(direction, damage, projectilePrefab, playerMask, 0);
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
