using UnityEngine;
using UnityEngine.UI;

// 캐릭터 머리 위 HP바. Health 의 OnHealthChanged 이벤트를 구독해 값이 바뀔 때만 갱신.
// 월드스페이스 Canvas + Image 를 런타임에 스스로 생성하므로 별도 아트 에셋/수동 배선 불필요.
// 바를 캐릭터 자식으로 만들어 이동 추종 + 사망(풀 비활성) 시 함께 사라짐이 자동 처리됨.
public class HealthBar : MonoBehaviour
{
    [Header("위치/크기 (캐릭터 로컬 단위)")]
    [Tooltip("캐릭터 피벗 기준 바의 오프셋(주로 머리 위로 +y)")]
    [SerializeField] private Vector2 offset = new Vector2(0f, 0.45f);

    [Tooltip("바의 로컬 가로/세로 크기")]
    [SerializeField] private Vector2 size = new Vector2(0.5f, 0.08f);

    [Header("색")]
    [SerializeField] private Color backColor = new Color(0.12f, 0.12f, 0.12f, 0.85f);
    [SerializeField] private Color fillColor = new Color(0.85f, 0.15f, 0.15f, 1f);

    [Header("정렬")]
    [Tooltip("스프라이트 위에 그려지도록 충분히 큰 값")]
    [SerializeField] private int sortingOrder = 100;

    // 베이스 픽셀 규격(월드 크기는 캔버스 localScale 로 매핑).
    private const float BaseWidth = 100f;
    private const float BaseHeight = 20f;
    private const float Pad = 2f;

    private Health health;
    private RectTransform fillRect;
    private bool built;

    private void Awake()
    {
        health = GetComponent<Health>();

        if (health == null)
        {
            Debug.LogError($"[HealthBar] {name}: 같은 오브젝트에 Health 컴포넌트가 없음 — 바 비활성.", this);

            enabled = false;

            return;
        }

        BuildBar();
    }

    private void OnEnable()
    {
        if (health == null) return;

        health.OnHealthChanged += HandleHealthChanged;

        // 구독 시점과 Health.OnEnable 발행 순서가 보장되지 않으므로, 현재값으로 즉시 1회 반영.
        HandleHealthChanged(health.CurrentHp(), health.MaxHp());
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (fillRect == null) return;

        float ratio = (max > 0) ? Mathf.Clamp01((float)current / max) : 0f;

        fillRect.localScale = new Vector3(ratio, 1f, 1f);
    }

    private void BuildBar()
    {
        if (built) return;

        built = true;

        // 루트: 월드스페이스 캔버스. 베이스 픽셀 규격을 로컬 size 로 매핑.
        var canvasGO = new GameObject("HealthBar_Canvas");

        canvasGO.transform.SetParent(transform, false);

        canvasGO.transform.localPosition = new Vector3(offset.x, offset.y, 0f);

        canvasGO.transform.localScale = new Vector3(size.x / BaseWidth, size.y / BaseHeight, 1f);

        var canvas = canvasGO.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.WorldSpace;

        canvas.sortingOrder = sortingOrder;

        var canvasRect = canvasGO.GetComponent<RectTransform>();

        canvasRect.sizeDelta = new Vector2(BaseWidth, BaseHeight);

        // 배경(어두운 바탕): 캔버스 전체를 채움.
        var bg = CreateImage("BG", canvasRect, backColor);

        bg.anchorMin = Vector2.zero;
        bg.anchorMax = Vector2.one;
        bg.offsetMin = Vector2.zero;
        bg.offsetMax = Vector2.zero;

        // 체력(빨강): 좌측 정렬 고정 크기 + localScale.x 로 채움 비율 표현.
        fillRect = CreateImage("Fill", bg, fillColor);

        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(0f, 0.5f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.sizeDelta = new Vector2(BaseWidth - Pad * 2f, BaseHeight - Pad * 2f);
        fillRect.anchoredPosition = new Vector2(Pad, 0f);
        fillRect.localScale = Vector3.one;
    }

    private RectTransform CreateImage(string goName, RectTransform parent, Color color)
    {
        var go = new GameObject(goName);

        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();

        img.color = color;

        img.raycastTarget = false;

        return img.rectTransform;
    }
}
