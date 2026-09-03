using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 인게임 HUD. 상단바: 라운드(좌) / 시간(중앙) / 골드(우). 하단좌: 장착 무기 아이콘 바(+레벨 칸).
// 데이터는 이벤트로만 갱신(폴링 없음): StageManager.OnStageChanged·OnTimeChanged, CurrencyWallet.OnChanged,
// WeaponSlotManager.OnWeaponsChanged. UI를 런타임 자가 생성 → 에디터 조립 불필요.
public class HUDManager : MonoBehaviour
{
    [Header("참조 (비우면 Player 태그에서 탐색)")]
    [SerializeField] private CurrencyWallet wallet;
    [SerializeField] private WeaponSlotManager slotManager;

    [Header("UI (선택)")]
    [Tooltip("비우면 내장 폰트 사용")]
    [SerializeField] private Font uiFont;

    private Text roundText;
    private Text timeText;
    private Text goldText;

    private RectTransform weaponBar;
    private Font font;

    // 무기 칸 색상.
    private static readonly Color CellBg = new Color(0f, 0f, 0f, 0.45f);
    private static readonly Color IconTint = Color.white;
    private static readonly Color PipOn = new Color(0.98f, 0.82f, 0.35f, 1f);
    private static readonly Color PipOff = new Color(1f, 1f, 1f, 0.22f);

    private const int SlotCellW = 84;
    private const int SlotCellH = 108;
    private const int SlotGap = 8;

    private void Awake()
    {
        if (wallet == null || slotManager == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                if (wallet == null) wallet = player.GetComponent<CurrencyWallet>();
                if (slotManager == null) slotManager = player.GetComponent<WeaponSlotManager>();
            }
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

        if (slotManager != null)
        {
            slotManager.OnWeaponsChanged += RefreshWeaponBar;
        }

        RefreshWeaponBar();
    }

    private void OnDestroy()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageChanged -= HandleStageChanged;
            StageManager.Instance.OnTimeChanged -= HandleTimeChanged;
        }

        if (wallet != null) wallet.OnChanged -= HandleGoldChanged;

        if (slotManager != null) slotManager.OnWeaponsChanged -= RefreshWeaponBar;
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

    // ---------- 무기 바 ----------

    // 슬롯 구성이 바뀔 때마다 통째로 다시 그림(슬롯 변경은 드묾 → 단순·안전 우선).
    private void RefreshWeaponBar()
    {
        if (weaponBar == null) return;

        for (int i = weaponBar.childCount - 1; i >= 0; i--)
        {
            Destroy(weaponBar.GetChild(i).gameObject);
        }

        if (slotManager == null) return;

        int n = Mathf.Max(1, slotManager.SlotCount);

        for (int i = 0; i < n; i++)
        {
            Weapon w = slotManager.GetSlot(i);

            BuildWeaponCell(i, w);
        }
    }

    // 한 칸: 배경 + (무기 있으면) 아이콘 + 레벨 칸(pip). 빈 칸은 배경만.
    private void BuildWeaponCell(int index, Weapon weapon)
    {
        var cell = CreateImage(weaponBar, $"Cell{index}", CellBg);
        var rt = cell.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.sizeDelta = new Vector2(SlotCellW, SlotCellH);
        rt.anchoredPosition = new Vector2(index * (SlotCellW + SlotGap), 0f);

        if (weapon == null) return;

        // 아이콘.
        var icon = CreateImage(cell.transform, "Icon", IconTint);
        icon.preserveAspect = true;
        icon.sprite = weapon.Icon;
        icon.enabled = weapon.Icon != null;
        var irt = icon.rectTransform;
        irt.anchorMin = new Vector2(0.5f, 1f);
        irt.anchorMax = new Vector2(0.5f, 1f);
        irt.pivot = new Vector2(0.5f, 1f);
        irt.sizeDelta = new Vector2(64f, 64f);
        irt.anchoredPosition = new Vector2(0f, -8f);

        // 레벨 칸(pip): 최대 레벨 수만큼 그리고, 현재 레벨만큼 채움.
        int max = Mathf.Clamp(weapon.MaxLevel, 1, 8);
        int lv = Mathf.Clamp(weapon.Level, 0, max);

        float pipGap = 3f;
        float totalGap = pipGap * (max - 1);
        float pipW = Mathf.Max(4f, (SlotCellW - 12f - totalGap) / max);
        float pipH = 8f;
        float rowW = pipW * max + totalGap;
        float startX = (SlotCellW - rowW) * 0.5f;

        for (int p = 0; p < max; p++)
        {
            var pip = CreateImage(cell.transform, $"Pip{p}", p < lv ? PipOn : PipOff);
            var prt = pip.rectTransform;
            prt.anchorMin = new Vector2(0f, 0f);
            prt.anchorMax = new Vector2(0f, 0f);
            prt.pivot = new Vector2(0f, 0f);
            prt.sizeDelta = new Vector2(pipW, pipH);
            prt.anchoredPosition = new Vector2(startX + p * (pipW + pipGap), 8f);
        }
    }

    // ---------- UI 생성 ----------

    private void BuildUI()
    {
        font = ResolveFont();

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
        roundText = CreateBox(canvasRect, "라운드  1",
            new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(200f, 64f));

        // 시간(상단 중앙)
        timeText = CreateBox(canvasRect, "시간  0",
            new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(240f, 64f));

        // 골드(우상)
        goldText = CreateBox(canvasRect, "골드  0",
            new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(200f, 64f));

        // 무기 바 컨테이너(좌하). 자식 칸을 좌→우로 배치.
        var barGO = new GameObject("WeaponBar");
        barGO.transform.SetParent(canvasRect, false);
        weaponBar = barGO.AddComponent<RectTransform>();
        weaponBar.anchorMin = new Vector2(0f, 0f);
        weaponBar.anchorMax = new Vector2(0f, 0f);
        weaponBar.pivot = new Vector2(0f, 0f);
        weaponBar.sizeDelta = new Vector2(600f, SlotCellH);
        weaponBar.anchoredPosition = new Vector2(20f, 20f);
    }

    // 흰 박스 + 검은 글자 한 칸 생성. anchor 는 (0,1)/(0.5,1)/(1,1) 로 좌·중·우상 정렬.
    private Text CreateBox(RectTransform parent, string content, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
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

    private Image CreateImage(Transform parent, string goName, Color color)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        return img;
    }

    private Font ResolveFont()
    {
        if (uiFont != null) return uiFont;

        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");

        if (f == null) f = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial", "돋움" }, 16);

        return f;
    }
}
