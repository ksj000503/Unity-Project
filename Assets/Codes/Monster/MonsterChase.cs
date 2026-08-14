using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MonsterChase : MonoBehaviour
{
    private Rigidbody2D rigid;

    [SerializeField]
    private float moveSpeed;

    private Transform target;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector2 direction = ((Vector2)target.position - rigid.position).normalized;

        rigid.MovePosition(rigid.position + direction * moveSpeed * Time.fixedDeltaTime);
    }

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("[MonsterChase] Player를 찾지 못했습니다");

            return;
        }

        target = player.transform;
    }
}
