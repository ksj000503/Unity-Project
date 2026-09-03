using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// 라운드(웨이브) 종료 시 뜨는 상점. StageManager.OnWaveCleared 로 열리고, 게임을 일시정지한다.
// UI(캔버스/카드/버튼)를 런타임에 스스로 생성 → 에디터에서 Canvas 조립 불필요.
// 레이아웃: 골드(상단) / 무기1·2·3(구매 카드) / 현재 스탯(우측) / 돌리기·다음 라운드(하단).
public class ShopManager : MonoBehaviour
{
    [Header("참조 (비우면 Player 태그에서 탐색)")]
    [SerializeField] private WeaponSlotManager slotManager;
    [SerializeField] private CurrencyWallet wallet;
    [SerializeField] private PlayerStats playerStats;

    [Header("상점 데이터")]
    [Tooltip("구매 후보 무기 풀(무기1·2·3 은 여기서 무작위 추첨)")]
    [SerializeField] private List<WeaponData> shopPool = new List<WeaponData>();

    [Tooltip("돌리기(리롤) 비용 — 임시 값")]
    [SerializeField] private int rerollCost = 5;

    [Tooltip("한 번에 제시할 카드 수")]
    [SerializeField] private int offerCount = 3;

    [Header("UI (선택)")]
    [Tooltip("비우면 내장 폰트 사용")]
    [SerializeField] private Font uiFont;

    // 카드 하나의 런타임 상태.
    private class Card
    {
        public Button button;
        public Text nameText;
        public Text priceText;
        public WeaponData weapon;
        public bool sold;
    }

    private Canvas canvas;
    private GameObject root;
    private Text goldText;
    private Text statsText;
    private Button rerollButton;
    private Text rerollText;
    private readonly List<Card> cards = new List<Card>();

    private bool isOpen;

    private void Awake()
    {
        ResolveRefs();

        BuildUI();

        SetVisible(false);
    }

    private void Start()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnWaveCleared += HandleWaveCleared;
        }
        else
        {
            Debug.LogWarning("[ShopManager] StageManager 없음 — 상점이 자동으로 열리지 않음.", this);
        }

        if (wallet != null) wallet.OnChanged += HandleWalletChanged;
        if (playerStats != null) playerStats.OnChanged += RefreshStats;
    }

    private void OnDestroy()
    {
        if (StageManager.Instance != null) StageManager.Instance.OnWaveCleared -= HandleWaveCleared;
        if (wallet != null) wallet.OnChanged -= HandleWalletChanged;
        if (playerStats != null) playerStats.OnChanged -= RefreshStats;
    }

    private void ResolveRefs()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            if (slotManager == null) slotManager = player.GetComponent<WeaponSlotManager>();
            if (wallet == null) wallet = player.GetComponent<CurrencyWallet>();
            if (playerStats == null) playerStats = player.GetComponent<PlayerStats>();
        }
    }

    // ---------- 열고 닫기 ----------

    private void HandleWaveCleared(int stage)
    {
        OpenShop();
    }

    private void OpenShop()
    {
        isOpen = true;

        SetVisible(true);

        Time.timeScale = 0f; // 상점 중 일시정지

        RollOffers();

        RefreshGold();

        RefreshStats();
    }

    private void CloseShopAndContinue()
    {
        if (!isOpen) return;

        isOpen = false;

        SetVisible(false);

        Time.timeScale = 1f;

        if (StageManager.Instance != null) StageManager.Instance.StartNextWave();
    }

    private void SetVisible(bool on)
    {
        if (root != null) root.SetActive(on);
    }

    // ---------- 상점 동작 ----------

    private void RollOffers()
    {
        // 풀을 섞어 카드마다 최대한 겹치지 않게 배정.
        // 풀이 카드 수보다 적으면(예: 2종·3카드) 앞 카드들은 서로 다른 무기로 채우고
        // 남는 카드만 무작위 반복 → 매 상점마다 보유 무기 종류가 최소 한 번씩은 노출됨.
        List<WeaponData> bag = null;

        if (shopPool != null && shopPool.Count > 0)
        {
            bag = new List<WeaponData>(shopPool);

            for (int i = bag.Count - 1; i > 0; i--) // Fisher-Yates 셔플
            {
                int j = Random.Range(0, i + 1);

                WeaponData tmp = bag[i];
                bag[i] = bag[j];
                bag[j] = tmp;
            }
        }

        for (int i = 0; i < cards.Count; i++)
        {
            Card c = cards[i];

            if (bag != null && bag.Count > 0)
            {
                c.weapon = (i < bag.Count) ? bag[i] : bag[Random.Range(0, bag.Count)];
            }
            else
            {
                c.weapon = null;
            }

            c.sold = false;

            UpdateCardVisual(c);
        }

        UpdateInteractable();
    }

    private void Buy(Card c)
    {
        if (!isOpen || c.sold || c.weapon == null || wallet == null || slotManager == null) return;

        int price = Mathf.Max(0, c.weapon.price);

        if (wallet.Coins < price) return;

        if (!wallet.TrySpend(price)) return;

        // 6슬롯 만석 + 신규 무기면 AddWeapon 실패 → 환불.
        bool ok = slotManager.AddWeapon(c.weapon);

        if (!ok)
        {
            wallet.Add(price);

            return;
        }

        c.sold = true;

        UpdateCardVisual(c);

        RefreshStats();

        UpdateInteractable();
    }

    private void Reroll()
    {
        if (!isOpen || wallet == null) return;

        if (wallet.Coins < rerollCost) return;

        if (!wallet.TrySpend(rerollCost)) return;

        RollOffers();
    }

    // ---------- 갱신 ----------

    private void HandleWalletChanged(int coins)
    {
        RefreshGold();
    }

    private void RefreshGold()
    {
        if (goldText != null) goldText.text = $"골드  {(wallet != null ? wallet.Coins : 0)}";

        UpdateInteractable();
    }

    private void RefreshStats()
    {
        if (statsText == null) return;

        if (playerStats == null)
        {
            statsText.text = "현재 스탯\n\n(PlayerStats 없음)";

            return;
        }

        statsText.text = "현재 스탯\n\n" + string.Join("\n", playerStats.GetStatLines());
    }

    private void UpdateCardVisual(Card c)
    {
        if (c.weapon == null)
        {
            c.nameText.text = (shopPool == null || shopPool.Count == 0) ? "(무기 풀 비었음)" : "-";
            c.priceText.text = "";

            return;
        }

        string label = string.IsNullOrEmpty(c.weapon.weaponName) ? c.weapon.name : c.weapon.weaponName;

        c.nameText.text = label;

        c.priceText.text = c.sold ? "판매완료" : $"{Mathf.Max(0, c.weapon.price)} G";
    }

    private void UpdateInteractable()
    {
        int coins = (wallet != null) ? wallet.Coins : 0;

        foreach (var c in cards)
        {
            bool buyable = !c.sold && c.weapon != null && coins >= Mathf.Max(0, c.weapon.price);

            if (c.button != null) c.button.interactable = buyable;
        }

        if (rerollButton != null) rerollButton.interactable = coins >= rerollCost;
    }

    // ---------- UI 생성 ----------

    private void BuildUI()
    {
        Font font = ResolveFont();

        EnsureEventSystem();

        root = new GameObject("ShopCanvas");
        root.transform.SetParent(transform, false);

        canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // 배경 딤(뒤 클릭 차단).
        var dim = CreateImage(root.transform, "Dim", new Color(0f, 0f, 0f, 0.6f));
        Stretch(dim.rectTransform);
        dim.raycastTarget = true;

        // 골드(상단 중앙, 무기2 위).
        goldText = CreateText(root.transform, "Gold", "골드  0", 40, font, TextAnchor.MiddleCenter);
        Place(goldText.rectTransform, new Vector2(-330f, 230f), new Vector2(240f, 70f));

        // 무기 카드 3장.
        float[] xs = { -600f, -330f, -60f };

        int count = Mathf.Max(1, offerCount); // 직렬화 값이 0이어도 최소 1장은 생성(카드 미표시 방지)

        for (int i = 0; i < count; i++)
        {
            float x = (i < xs.Length) ? xs[i] : (-600f + i * 270f);

            Card card = CreateCard(root.transform, font, new Vector2(x, 10f), new Vector2(240f, 380f), i + 1);

            cards.Add(card);
        }

        // 현재 스탯 패널(우측).
        var statsPanel = CreateImage(root.transform, "StatsPanel", new Color(0.1f, 0.1f, 0.12f, 0.95f));
        Place(statsPanel.rectTransform, new Vector2(480f, 0f), new Vector2(340f, 470f));

        statsText = CreateText(statsPanel.transform, "StatsText", "현재 스탯", 30, font, TextAnchor.UpperLeft);
        Stretch(statsText.rectTransform, 20f);

        // 돌리기 버튼(하단).
        rerollButton = CreateButton(root.transform, font, $"돌리기 ({rerollCost}G)", new Vector2(-330f, -260f), new Vector2(200f, 70f), out rerollText);
        rerollButton.onClick.AddListener(Reroll);

        // 다음 라운드 버튼(돌리기 옆).
        Button nextButton = CreateButton(root.transform, font, "다음 라운드", new Vector2(-90f, -260f), new Vector2(200f, 70f), out _);
        nextButton.onClick.AddListener(CloseShopAndContinue);
    }

    private Card CreateCard(Transform parent, Font font, Vector2 pos, Vector2 size, int number)
    {
        var img = CreateImage(parent, $"Weapon{number}", new Color(0.85f, 0.85f, 0.85f, 1f));
        Place(img.rectTransform, pos, size);

        var button = img.gameObject.AddComponent<Button>();
        button.targetGraphic = img;

        var nameText = CreateText(img.transform, "Name", $"무기{number}", 34, font, TextAnchor.MiddleCenter);
        Place(nameText.rectTransform, new Vector2(0f, 20f), new Vector2(size.x - 20f, 80f));
        nameText.color = Color.black;

        var priceText = CreateText(img.transform, "Price", "", 30, font, TextAnchor.MiddleCenter);
        Place(priceText.rectTransform, new Vector2(0f, -size.y * 0.5f + 40f), new Vector2(size.x - 20f, 60f));
        priceText.color = new Color(0.15f, 0.35f, 0.15f);

        var card = new Card { button = button, nameText = nameText, priceText = priceText };

        button.onClick.AddListener(() => Buy(card));

        return card;
    }

    // ---------- UI 헬퍼 ----------

    // UI 클릭 처리에 필요한 EventSystem 이 씬에 없으면 생성(신규 Input System 모듈 사용).
    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        if (FindAnyObjectByType<EventSystem>() != null) return;

        var es = new GameObject("EventSystem");

        es.AddComponent<EventSystem>();

        es.AddComponent<InputSystemUIInputModule>();
    }

    private Font ResolveFont()
    {
        if (uiFont != null) return uiFont;

        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 빌트인 폰트가 null 이면(에디터/버전 이슈) OS 폰트로 폴백 → 카드·버튼 텍스트가 통째로 안 보이는 문제 방지.
        if (f == null) f = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial", "돋움" }, 16);

        if (f == null) Debug.LogWarning("[ShopManager] 폰트 로드 실패 — 카드/버튼 텍스트가 보이지 않을 수 있습니다.", this);

        return f;
    }

    private Image CreateImage(Transform parent, string goName, Color color)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = color;

        return img;
    }

    private Text CreateText(Transform parent, string goName, string content, int fontSize, Font font, TextAnchor anchor)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(parent, false);

        var t = go.AddComponent<Text>();
        t.text = content;
        t.font = font;
        t.fontSize = fontSize;
        t.alignment = anchor;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;

        return t;
    }

    private Button CreateButton(Transform parent, Font font, string label, Vector2 pos, Vector2 size, out Text labelText)
    {
        var img = CreateImage(parent, "Button", new Color(0.2f, 0.45f, 0.85f, 1f));
        Place(img.rectTransform, pos, size);

        var button = img.gameObject.AddComponent<Button>();
        button.targetGraphic = img;

        labelText = CreateText(img.transform, "Label", label, 30, font, TextAnchor.MiddleCenter);
        Stretch(labelText.rectTransform);

        return button;
    }

    private void Place(RectTransform rt, Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
    }

    private void Stretch(RectTransform rt, float padding = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }
}
