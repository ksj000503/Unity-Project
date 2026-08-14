using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private float speed;

    Rigidbody2D rigidBody;

    Vector2 Move;

    [SerializeField]
    private MapData mapData;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();        
    }

    public void OnMove(InputValue value)
    {
        Move = value.Get<Vector2>();
    }

    void FixedUpdate()
    {
        Vector2 current = rigidBody.position;

        Vector2 target = current + (Move * speed *Time.fixedDeltaTime);

        rigidBody.MovePosition(target);
    }
}
