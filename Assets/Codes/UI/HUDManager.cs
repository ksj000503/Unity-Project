using UnityEngine;
using UnityEngine.UI;

// 인게임 HUD 상단바: 라운드(좌) / 시간(중앙) / 골드(우).
// 데이터는 이벤트로만 갱신(폴링 없음): StageManager.OnStageChanged·OnTimeChanged, CurrencyWallet.OnChanged.
// UI를 런타임 자가 생성 → 에디터 조립 불필요. 컴포넌트 하나만 붙이면 동작.
public class HUDManager : MonoBehaviour
{
    [Header("참조 (비우면 Player 태그에서 탐색)")]
    [SerializeField] private CurrencyWallet wallet;

    [Header("UI (선택)")]
    [Tooltip("비우면 내장 폰트 사용")]
    [SerializeField] private Font uiFont;

    private Text roundText;
    private Text timeText;
    private Text goldText;

    private void Awake()
    {
        if (wallet == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null) wallet = player.GetComponent<CurrencyWallet>();
        }

        BuildUI();
    }

    private void Start()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageChanged += HandleStageChanged;
            StageManager.Instance.OnTimeChanged += HandleTimeChanged;

            HandleStageChanged(StageManager.Instance.CurrentStage);
            HandleTimeChanged(StageManager.Instance.TimeRemaining);
        }
        else
        {
            Debug.LogWarning("[HUDManager] StageManager 없음 — 라운드/시간 표시 불가.", this);
        }

        if (wallet != null)
        {
            wallet.OnChanged += HandleGoldChanged;

            HandleGoldChanged(wallet.Coins);
        }
        else
        {
            HandleGoldChanged(0);
        }
    }

    private void OnDestroy()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageChanged -= HandleStageChanged;
            StageManager.Instance.OnTimeChanged -= HandleTimeChanged;
        }

        if (wallet != null) wallet.OnChanged -= HandleGoldChanged;
    }

    private void HandleStageChanged(int stage)
    {
        if (roundText != null) roundText.text = $"라운드  {stage}";
    }

    private void HandleTimeChanged(float remaining)
    {
        if (timeText != null) timeText.text = $"시간  {Mathf.CeilToInt(Mathf.Max(0f, remaining))}";
    }

    private void HandleGoldChanged(int coins)
    {
        if (goldText != null) goldText.text = $"골드  {coins}";
    }

    // ---------- UI 생성 ----------

    private void BuildUI()
    {
        Font font = ResolveFont();

        var root = new GameObject("HUDCanvas");
        root.transform.SetParent(transform, false);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // 상점(200)보다 아래

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = root.GetComponent<RectTransform>();

        // 라운드(좌상)
        roundText = CreateBox(canvasRect, font, "라운드  1",
            new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(200f, 64f));

        // 시간(상단 중앙)
        timeText = CreateBox(canvasRect, font, "시간  0",
            new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(240f, 64f));

        // 골드(우상)
        goldText = CreateBox(canvasRect, font, "골드  0",
            new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(200f, 64f));
    }

    // 흰 박스 + 검은 글자 한 칸 생성. anchor 는 (0,1)/(0.5,1)/(1,1) 로 좌·중·우상 정렬.
    private Text CreateBox(RectTransform parent, Font font, string content, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        var boxGO = new GameObject("HUDBox");
        boxGO.transform.SetParent(parent, false);

        var img = boxGO.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.92f);
        img.raycastTarget = false;

        var rt = img.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(boxGO.transform, false);

        var t = textGO.AddComponent<Text>();
        t.text = content;
        t.font = font;
        t.fontSize = 32;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.black;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;

        var trt = t.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        return t;
    }

    private Font ResolveFont()
    {
        if (uiFont != null) return uiFont;

        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return f;
    }
}
