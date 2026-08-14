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

        if(sqrDistance > attackRange * attackRange)
        {
            return;
        }

        cooldownTimer = attackCooldown;
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
