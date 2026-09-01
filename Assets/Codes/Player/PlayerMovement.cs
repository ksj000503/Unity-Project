using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private float speed;

    Rigidbody2D rigidBody;

    Vector2 Move;

    [SerializeField]
    private MapData mapData; // 카메라 없을 때 폴백용

    private Camera cam;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
    }

    private Camera GetCam()
    {
        if (cam != null) return cam;

        cam = Camera.main;

        if (cam == null) cam = FindAnyObjectByType<Camera>();

        return cam;
    }

    public void OnMove(InputValue value)
    {
        Move = value.Get<Vector2>();
    }

    void FixedUpdate()
    {
        Vector2 current = rigidBody.position;

        Vector2 target = current + (Move * speed * Time.fixedDeltaTime);

        // 이동 범위 = 카메라 화면. 카메라 Size 를 키우면 이동 범위도 그만큼 넓어짐.
        Camera c = GetCam();

        if (c != null && c.orthographic)
        {
            float halfH = c.orthographicSize;
            float halfW = c.orthographicSize * c.aspect;
            Vector2 center = c.transform.position;

            target.x = Mathf.Clamp(target.x, center.x - halfW, center.x + halfW);
            target.y = Mathf.Clamp(target.y, center.y - halfH, center.y + halfH);
        }
        else if (mapData != null)
        {
            target.x = Mathf.Clamp(target.x, mapData.minBounds.x, mapData.maxBounds.x);
            target.y = Mathf.Clamp(target.y, mapData.minBounds.y, mapData.maxBounds.y);
        }

        rigidBody.MovePosition(target);
    }
}
