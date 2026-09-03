using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// 라운드(웨이브) 종료 시 뜨는 상점. StageManager.OnWaveCleared 로 열리고, 게임을 일시정지한다.
// UI(캔버스/카드/버튼)를 런타임에 스스로 생성 → 에디터에서 Canvas 조립 불필요.
// 카드는 무기 또는 아이템(혼합). 아이템은 행운 가중치로 등급을 뽑고 구매 시 PlayerStats 에 효과 적용.
public class ShopManager : MonoBehaviour
{
    [Header("참조 (비우면 Player 태그에서 탐색)")]
    [SerializeField] private WeaponSlotManager slotManager;
    [SerializeField] private CurrencyWallet wallet;
    [SerializeField] private PlayerStats playerStats;

    [Header("상점 데이터")]
    [Tooltip("구매 후보 무기 풀")]
    [SerializeField] private List<WeaponData> shopPool = new List<WeaponData>();

    [Tooltip("구매 후보 아이템 풀(등급별로 여러 개 넣으면 행운 뽑기가 반영됨)")]
    [SerializeField] private List<ItemData> itemPool = new List<ItemData>();

    [Tooltip("각 카드가 아이템으로 나올 확률(0~1). 나머지는 무기")]
    [Range(0f, 1f)]
    [SerializeField] private float itemChance = 0.4f;

    [Tooltip("돌리기(리롤) 비용 — 임시 값")]
    [SerializeField] private int rerollCost = 5;

    [Tooltip("한 번에 제시할 카드 수")]
    [SerializeField] private int offerCount = 3;

    [Header("UI (선택)")]
    [Tooltip("비우면 내장 폰트 사용")]
    [SerializeField] private Font uiFont;

    // 등급 카드 배경색: Normal=회색, Epic=보라, Unique=금색.
    private static readonly Color[] RarityColor =
    {
        new Color(0.85f, 0.85f, 0.85f, 1f),
        new Color(0.74f, 0.56f, 0.96f, 1f),
        new Color(0.98f, 0.82f, 0.35f, 1f),
    };

    // 카드 하나의 런타임 상태. weapon 또는 item 중 하나만 채워짐.
    private class Card
    {
        public Button button;
        public Image bg;
        public Text nameText;
        public Text priceText;
        public WeaponData weapon;
        public ItemData item;
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
        bool canWeapon = shopPool != null && shopPool.Count > 0;
        bool canItem = itemPool != null && itemPool.Count > 0;

        for (int i = 0; i < cards.Count; i++)
        {
            Card c = cards[i];

            c.weapon = null;
            c.item = null;

            bool asItem;

            if (canItem && canWeapon) asItem = Random.value < itemChance;
            else asItem = canItem && !canWeapon;

            if (asItem)
            {
                c.item = RollItem();
            }
            else if (canWeapon)
            {
                c.weapon = shopPool[Random.Range(0, shopPool.Count)];
            }

            c.sold = false;

            UpdateCardVisual(c);
        }

        UpdateInteractable();
    }

    // 행운 가중치로 등급을 뽑고, 해당 등급 아이템 중 하나를 무작위 선택.
    private ItemData RollItem()
    {
        if (itemPool == null || itemPool.Count == 0) return null;

        int luck = (playerStats != null) ? playerStats.Luck : 0;

        float wN = HasRarity(ItemRarity.Normal) ? 70f : 0f;
        float wE = HasRarity(ItemRarity.Epic) ? 25f + luck * 2f : 0f;
        float wU = HasRarity(ItemRarity.Unique) ? 5f + luck : 0f;

        float total = wN + wE + wU;

        ItemRarity chosen = ItemRarity.Normal;

        if (total > 0f)
        {
            float r = Random.value * total;

            if (r < wN) chosen = ItemRarity.Normal;
            else if (r < wN + wE) chosen = ItemRarity.Epic;
            else chosen = ItemRarity.Unique;
        }

        List<ItemData> pick = new List<ItemData>();

        foreach (var it in itemPool)
        {
            if (it != null && it.rarity == chosen) pick.Add(it);
        }

        if (pick.Count == 0) // 해당 등급이 풀에 없으면 전체에서
        {
            foreach (var it in itemPool) if (it != null) pick.Add(it);
        }

        return pick.Count > 0 ? pick[Random.Range(0, pick.Count)] : null;
    }

    private bool HasRarity(ItemRarity r)
    {
        foreach (var it in itemPool) if (it != null && it.rarity == r) return true;

        return false;
    }

    private void Buy(Card c)
    {
        if (!isOpen || c.sold || wallet == null) return;

        if (c.weapon != null)
        {
            if (slotManager == null) return;

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
        }
        else if (c.item != null)
        {
            int price = Mathf.Max(0, c.item.price);

            if (wallet.Coins < price) return;

            if (!wallet.TrySpend(price)) return;

            ApplyItem(c.item);

            c.sold = true;
        }
        else
        {
            return;
        }

        UpdateCardVisual(c);

        RefreshStats();

        UpdateInteractable();
    }

    // 아이템 효과를 PlayerStats 에 적용.
    private void ApplyItem(ItemData it)
    {
        if (playerStats == null || it == null)
        {
            Debug.LogWarning("[ShopManager] PlayerStats/ItemData 없음 — 아이템 효과 미적용.", this);

            return;
        }

        if (it.effects == null) return;

        foreach (var e in it.effects)
        {
            switch (e.stat)
            {
                case PlayerStatType.DamageUp: playerStats.AddDamagePercent(e.amount); break;
                case PlayerStatType.CooldownDown: playerStats.AddCooldownReduction(e.amount); break;
                case PlayerStatType.MaxHpUp: playerStats.AddMaxHp(Mathf.RoundToInt(e.amount)); break;
                case PlayerStatType.LuckUp: playerStats.AddLuck(Mathf.RoundToInt(e.amount)); break;
            }
        }
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
        // 무기 카드
        if (c.weapon != null)
        {
            if (c.bg != null) c.bg.color = RarityColor[0];

            string label = string.IsNullOrEmpty(c.weapon.weaponName) ? c.weapon.name : c.weapon.weaponName;

            c.nameText.text = label;

            c.priceText.text = c.sold ? "판매완료" : $"{Mathf.Max(0, c.weapon.price)} G";

            return;
        }

        // 아이템 카드
        if (c.item != null)
        {
            if (c.bg != null) c.bg.color = RarityColor[Mathf.Clamp((int)c.item.rarity, 0, 2)];

            string label = string.IsNullOrEmpty(c.item.itemName) ? c.item.name : c.item.itemName;

            c.nameText.text = $"[{RarityName(c.item.rarity)}]\n{label}\n\n{EffectSummary(c.item)}";

            c.priceText.text = c.sold ? "판매완료" : $"{Mathf.Max(0, c.item.price)} G";

            return;
        }

        // 빈 카드
        if (c.bg != null) c.bg.color = RarityColor[0];

        bool poolsEmpty = (shopPool == null || shopPool.Count == 0) && (itemPool == null || itemPool.Count == 0);

        c.nameText.text = poolsEmpty ? "(풀 비었음)" : "-";

        c.priceText.text = "";
    }

    private static string RarityName(ItemRarity r)
    {
        switch (r)
        {
            case ItemRarity.Epic: return "에픽";
            case ItemRarity.Unique: return "유니크";
            default: return "노말";
        }
    }

    private static string EffectSummary(ItemData it)
    {
        if (it.effects == null || it.effects.Count == 0) return "";

        List<string> lines = new List<string>();

        foreach (var e in it.effects)
        {
            switch (e.stat)
            {
                case PlayerStatType.DamageUp: lines.Add($"데미지 +{e.amount:0.#}%"); break;
                case PlayerStatType.CooldownDown: lines.Add($"쿨감 {e.amount:0.#}%"); break;
                case PlayerStatType.MaxHpUp: lines.Add($"HP +{e.amount:0}"); break;
                case PlayerStatType.LuckUp: lines.Add($"행운 +{e.amount:0}"); break;
            }
        }

        return string.Join("\n", lines);
    }

    private void UpdateInteractable()
    {
        int coins = (wallet != null) ? wallet.Coins : 0;

        foreach (var c in cards)
        {
            int price = c.weapon != null ? c.weapon.price : (c.item != null ? c.item.price : int.MaxValue);

            bool hasOffer = c.weapon != null || c.item != null;

            bool buyable = !c.sold && hasOffer && coins >= Mathf.Max(0, price);

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

        // 골드(상단 중앙).
        goldText = CreateText(root.transform, "Gold", "골드  0", 40, font, TextAnchor.MiddleCenter);
        Place(goldText.rectTransform, new Vector2(-330f, 230f), new Vector2(240f, 70f));

        // 카드 3장.
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
        var img = CreateImage(parent, $"Card{number}", RarityColor[0]);
        Place(img.rectTransform, pos, size);

        var button = img.gameObject.AddComponent<Button>();
        button.targetGraphic = img;

        // 이름/설명(멀티라인). 아이템은 [등급]\n이름\n\n효과 형태로 들어감.
        var nameText = CreateText(img.transform, "Name", $"카드{number}", 28, font, TextAnchor.MiddleCenter);
        Place(nameText.rectTransform, new Vector2(0f, 26f), new Vector2(size.x - 16f, 260f));
        nameText.color = Color.black;

        var priceText = CreateText(img.transform, "Price", "", 30, font, TextAnchor.MiddleCenter);
        Place(priceText.rectTransform, new Vector2(0f, -size.y * 0.5f + 40f), new Vector2(size.x - 20f, 60f));
        priceText.color = new Color(0.12f, 0.28f, 0.12f);

        var card = new Card { button = button, bg = img, nameText = nameText, priceText = priceText };

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
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
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
