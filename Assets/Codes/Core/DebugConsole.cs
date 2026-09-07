using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

// QA/디버그용 인게임 콘솔. F1 로 열고 닫는다. 런타임에 UI 를 스스로 만들고
// 에디터/개발빌드에서만 자동 생성되므로 씬 배선도, 배포 빌드 오염도 없다.
// 기존 매니저(StageManager/Health/CurrencyWallet/WeaponSlotManager)를 그대로 호출.
public class DebugConsole : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<DebugConsole>() != null) return;

        var go = new GameObject("DebugConsole");

        DontDestroyOnLoad(go);

        go.AddComponent<DebugConsole>();
    }
#endif

    private GameObject panel;
    private Font font;
    private bool open;

    private Text invincibleLabel;
    private Text statusLabel;

    // 플레이어 관련 참조(런타임에 지연 탐색).
    private CurrencyWallet wallet;
    private Health playerHealth;
    private WeaponSlotManager slots;

    private float nextY;

    private void Awake()
    {
        font = ResolveFont();

        BuildUI();

        SetOpen(false);
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;

        if (kb != null && kb.f1Key.wasPressedThisFrame) SetOpen(!open);
    }

    private void SetOpen(bool on)
    {
        open = on;

        if (panel != null) panel.SetActive(on);

        if (on) RefreshLabels();
    }

    // ---------- 참조 ----------

    private void Resolve()
    {
        if (wallet != null && playerHealth != null && slots != null) return;

        GameObject p = GameObject.FindGameObjectWithTag("Player");

        if (p == null) return;

        if (wallet == null) wallet = p.GetComponent<CurrencyWallet>();
        if (playerHealth == null) playerHealth = p.GetComponent<Health>();
        if (slots == null) slots = p.GetComponent<WeaponSlotManager>();
    }

    // ---------- 액션 ----------

    private void GiveGold()
    {
        Resolve();

        if (wallet != null) wallet.Add(100);

        RefreshLabels();
    }

    private void NextStage()
    {
        if (StageManager.Instance == null) return;

        StageManager.Instance.DebugJumpToStage(StageManager.Instance.CurrentStage + 1);

        RefreshLabels();
    }

    private void JumpToBoss()
    {
        if (StageManager.Instance == null) return;

        // 보스 주기는 스포너 기본값 5 기준. 다음 5의 배수 스테이지로 점프 → 즉시 보스 등장.
        int cur = StageManager.Instance.CurrentStage;

        int target = ((cur / 5) + 1) * 5;

        StageManager.Instance.DebugJumpToStage(target);

        RefreshLabels();
    }

    private void ToggleInvincible()
    {
        Resolve();

        if (playerHealth != null) playerHealth.Invincible = !playerHealth.Invincible;

        RefreshLabels();
    }

    private void KillPlayer()
    {
        Resolve();

        if (playerHealth != null) playerHealth.Kill();
    }

    private void SetTimeScale(float s)
    {
        Time.timeScale = s;

        RefreshLabels();
    }

    private void GiveWeapon(WeaponData data)
    {
        Resolve();

        if (slots != null && data != null) slots.AddWeapon(data);
    }

    private void RefreshLabels()
    {
        Resolve();

        if (invincibleLabel != null)
            invincibleLabel.text = "무적: " + ((playerHealth != null && playerHealth.Invincible) ? "ON" : "OFF");

        if (statusLabel != null)
        {
            int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStage : 0;
            int gold = wallet != null ? wallet.Coins : 0;
            statusLabel.text = $"스테이지 {stage}  |  골드 {gold}  |  배속 {Time.timeScale:0.##}x";
        }
    }

    // ---------- UI ----------

    private void BuildUI()
    {
        EnsureEventSystem();

        var root = new GameObject("DebugCanvas");
        root.transform.SetParent(transform, false);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // 최상단

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // 패널(우측).
        panel = new GameObject("Panel").AddComponent<Image>().gameObject;
        panel.transform.SetParent(root.transform, false);
        var pimg = panel.GetComponent<Image>();
        pimg.color = new Color(0f, 0f, 0f, 0.82f);
        var prt = pimg.rectTransform;
        prt.anchorMin = new Vector2(1f, 0f);
        prt.anchorMax = new Vector2(1f, 1f);
        prt.pivot = new Vector2(1f, 1f);
        prt.sizeDelta = new Vector2(340f, 0f);
        prt.anchoredPosition = new Vector2(0f, 0f);

        nextY = -20f;

        MakeText("DEBUG 콘솔 (F1)", 26, new Color(1f, 0.85f, 0.3f));
        statusLabel = MakeText("", 18, new Color(0.8f, 0.85f, 0.95f));

        MakeButton("골드 +100", GiveGold);
        MakeButton("스테이지 +1", NextStage);
        MakeButton("보스 스테이지로 점프", JumpToBoss);
        invincibleLabel = null;
        MakeToggleButton();
        MakeButton("즉사 (게임오버)", KillPlayer);

        MakeText("배속", 18, new Color(0.8f, 0.85f, 0.95f));
        MakeSpeedRow();

        MakeText("무기 지급", 18, new Color(0.8f, 0.85f, 0.95f));
        BuildWeaponButtons();
    }

    private void MakeToggleButton()
    {
        var b = MakeButton("무적: OFF", ToggleInvincible);
        invincibleLabel = b.GetComponentInChildren<Text>();
    }

    // 세로 스택에 한 줄씩 쌓는다.
    private Text MakeText(string s, int size, Color color)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(panel.transform, false);
        var t = go.AddComponent<Text>();
        t.text = s; t.font = font; t.fontSize = size; t.color = color;
        t.alignment = TextAnchor.MiddleLeft;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        Place(t.rectTransform, new Vector2(16f, nextY), new Vector2(300f, 26f));
        nextY -= 30f;
        return t;
    }

    private Button MakeButton(string label, UnityEngine.Events.UnityAction onClick)
    {
        var img = new GameObject("Btn").AddComponent<Image>();
        img.transform.SetParent(panel.transform, false);
        img.color = new Color(0.2f, 0.35f, 0.6f, 1f);
        Place(img.rectTransform, new Vector2(16f, nextY), new Vector2(300f, 40f));

        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(onClick);

        var t = new GameObject("Label").AddComponent<Text>();
        t.transform.SetParent(img.transform, false);
        t.text = label; t.font = font; t.fontSize = 20; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        Stretch(t.rectTransform);

        nextY -= 46f;
        return b;
    }

    private void MakeSpeedRow()
    {
        float[] speeds = { 0.5f, 1f, 2f, 4f };
        float x = 16f;
        float w = 70f;
        foreach (float sp in speeds)
        {
            var img = new GameObject("Spd").AddComponent<Image>();
            img.transform.SetParent(panel.transform, false);
            img.color = new Color(0.3f, 0.3f, 0.35f, 1f);
            Place(img.rectTransform, new Vector2(x, nextY), new Vector2(w, 38f));
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            float captured = sp;
            b.onClick.AddListener(() => SetTimeScale(captured));
            var t = new GameObject("L").AddComponent<Text>();
            t.transform.SetParent(img.transform, false);
            t.text = sp + "x"; t.font = font; t.fontSize = 18; t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            Stretch(t.rectTransform);
            x += w + 6f;
        }
        nextY -= 44f;
    }

    private void BuildWeaponButtons()
    {
        ShopCatalog cat = Resources.Load<ShopCatalog>("ShopCatalog");

        if (cat == null || cat.weapons == null) return;

        float x = 16f;
        float w = 147f;
        bool left = true;
        foreach (var wd in cat.weapons)
        {
            if (wd == null) continue;
            string nm = string.IsNullOrEmpty(wd.weaponName) ? wd.name : wd.weaponName;

            var img = new GameObject("W").AddComponent<Image>();
            img.transform.SetParent(panel.transform, false);
            img.color = new Color(0.25f, 0.45f, 0.3f, 1f);
            Place(img.rectTransform, new Vector2(left ? 16f : 16f + w + 6f, nextY), new Vector2(w, 36f));
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            WeaponData captured = wd;
            b.onClick.AddListener(() => GiveWeapon(captured));
            var t = new GameObject("L").AddComponent<Text>();
            t.transform.SetParent(img.transform, false);
            t.text = nm; t.font = font; t.fontSize = 16; t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            Stretch(t.rectTransform);

            if (!left) nextY -= 40f;
            left = !left;
        }
        if (!left) nextY -= 40f;
    }

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
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (f == null) f = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial", "돋움" }, 16);
        return f;
    }

    private void Place(RectTransform rt, Vector2 topLeftPos, Vector2 size)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = size;
        rt.anchoredPosition = topLeftPos;
    }

    private void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
