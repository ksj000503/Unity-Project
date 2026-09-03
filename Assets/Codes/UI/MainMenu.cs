using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

// 시작 로비. 타이틀 + 시작 무기 선택 + 게임시작/종료 버튼을 런타임에 스스로 생성한다.
// 에셋(타이틀 텍스처·버튼 스프라이트·무기 목록)은 Resources/LobbyConfig.asset 에서 읽어 씬 배선이 필요 없다.
// "게임 시작" → 고른 무기를 GameConfig 에 담고 게임 씬 로드 → GameBootstrap 이 플레이어에게 지급.
public class MainMenu : MonoBehaviour
{
    [Header("설정 (비우면 Resources/LobbyConfig 자동 로드)")]
    [SerializeField] private LobbyConfig config;

    [Tooltip("게임 씬 이름")]
    [SerializeField] private string gameSceneName = "SampleScene";

    [Header("UI (선택)")]
    [SerializeField] private Font uiFont;

    private readonly List<Button> weaponButtons = new List<Button>();
    private readonly List<Image> weaponButtonBg = new List<Image>();
    private List<WeaponData> weapons = new List<WeaponData>();
    private int selectedIndex = -1;

    private Text selectedLabel;
    private Button startButton;

    private static readonly Color CardIdle = new Color(0.20f, 0.22f, 0.28f, 1f);
    private static readonly Color CardSelected = new Color(0.98f, 0.82f, 0.35f, 1f);

    private void Awake()
    {
        if (config == null) config = Resources.Load<LobbyConfig>("LobbyConfig");

        if (config != null && config.startingWeapons != null)
        {
            foreach (var w in config.startingWeapons)
            {
                if (w != null) weapons.Add(w);
            }
        }

        if (weapons.Count > 0) selectedIndex = 0;

        BuildUI();

        RefreshSelection();
    }

    // ---------- 동작 ----------

    private void SelectWeapon(int index)
    {
        if (index < 0 || index >= weapons.Count) return;

        selectedIndex = index;

        RefreshSelection();
    }

    private void RefreshSelection()
    {
        for (int i = 0; i < weaponButtonBg.Count; i++)
        {
            if (weaponButtonBg[i] != null)
                weaponButtonBg[i].color = (i == selectedIndex) ? CardSelected : CardIdle;
        }

        if (selectedLabel != null)
        {
            if (selectedIndex >= 0 && selectedIndex < weapons.Count)
            {
                WeaponData w = weapons[selectedIndex];

                string nm = string.IsNullOrEmpty(w.weaponName) ? w.name : w.weaponName;

                selectedLabel.text = $"선택: {nm}   (데미지 {w.damage} / 쿨 {w.attackCooldown:0.##}s)";
            }
            else
            {
                selectedLabel.text = weapons.Count == 0 ? "(시작 무기 목록이 비어 있음)" : "무기를 선택하세요";
            }
        }

        if (startButton != null) startButton.interactable = true; // 무기 없이도 시작은 허용(빈손 플레이)
    }

    private void StartGame()
    {
        GameConfig.StartingWeapon =
            (selectedIndex >= 0 && selectedIndex < weapons.Count) ? weapons[selectedIndex] : null;

        Time.timeScale = 1f;

        SceneManager.LoadScene(gameSceneName);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- UI 생성 ----------

    private void BuildUI()
    {
        Font font = ResolveFont();

        EnsureEventSystem();

        var root = new GameObject("MainMenuCanvas");
        root.transform.SetParent(transform, false);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // 배경.
        var bg = CreateImage(root.transform, "BG", new Color(0.08f, 0.09f, 0.12f, 1f));
        Stretch(bg.rectTransform);

        // 타이틀: 텍스처가 있으면 RawImage(통짜 로고), 없으면 텍스트.
        if (config != null && config.titleTexture != null)
        {
            var raw = new GameObject("Title").AddComponent<RawImage>();
            raw.transform.SetParent(root.transform, false);
            raw.texture = config.titleTexture;

            float tw = config.titleTexture.width;
            float th = config.titleTexture.height;
            float scale = (tw > 0f) ? Mathf.Min(1f, 900f / tw) : 1f;

            Place(raw.rectTransform, new Vector2(0f, 320f), new Vector2(tw * scale, th * scale));
        }
        else
        {
            var title = CreateText(root.transform, "Title", "POTATO SURVIVORS", 96, font, TextAnchor.MiddleCenter);
            Place(title.rectTransform, new Vector2(0f, 320f), new Vector2(1400f, 180f));
            title.color = new Color(0.98f, 0.82f, 0.35f);
        }

        // 안내.
        var guide = CreateText(root.transform, "Guide", "시작 무기를 고르세요", 40, font, TextAnchor.MiddleCenter);
        Place(guide.rectTransform, new Vector2(0f, 150f), new Vector2(1000f, 60f));
        guide.color = new Color(0.85f, 0.85f, 0.9f);

        // 무기 선택 버튼(가로 배치).
        BuildWeaponButtons(root.transform, font);

        // 선택 요약 라벨.
        selectedLabel = CreateText(root.transform, "Selected", "", 32, font, TextAnchor.MiddleCenter);
        Place(selectedLabel.rectTransform, new Vector2(0f, -120f), new Vector2(1400f, 50f));
        selectedLabel.color = new Color(0.98f, 0.82f, 0.35f);

        // 시작 버튼(스프라이트 있으면 스프라이트, 없으면 색 버튼).
        startButton = BuildActionButton(root.transform, font, "게임 시작",
            config != null ? config.startSprite : null,
            new Vector2(0f, -240f), new Vector2(420f, 130f), new Color(0.2f, 0.55f, 0.3f, 1f));
        startButton.onClick.AddListener(StartGame);

        // 종료 버튼.
        Button quit = BuildActionButton(root.transform, font, "종료",
            config != null ? config.quitSprite : null,
            new Vector2(0f, -380f), new Vector2(420f, 130f), new Color(0.5f, 0.25f, 0.25f, 1f));
        quit.onClick.AddListener(QuitGame);
    }

    private void BuildWeaponButtons(Transform parent, Font font)
    {
        int n = weapons.Count;

        if (n == 0) return;

        float cardW = 200f;
        float gap = 24f;
        float totalW = n * cardW + (n - 1) * gap;
        float startX = -totalW * 0.5f + cardW * 0.5f;

        for (int i = 0; i < n; i++)
        {
            WeaponData w = weapons[i];

            float x = startX + i * (cardW + gap);

            var img = CreateImage(parent, $"Weapon{i}", CardIdle);
            Place(img.rectTransform, new Vector2(x, 20f), new Vector2(cardW, 200f));

            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;

            string nm = string.IsNullOrEmpty(w.weaponName) ? w.name : w.weaponName;
            string kind = w.weaponType == WeaponType.RangedShoot ? "원거리" : "근접";

            var label = CreateText(img.transform, "Label", $"{nm}\n\n[{kind}]\n데미지 {w.damage}", 26, font, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 10f);
            label.color = Color.white;

            int captured = i;
            button.onClick.AddListener(() => SelectWeapon(captured));

            weaponButtons.Add(button);
            weaponButtonBg.Add(img);
        }
    }

    // 스프라이트가 있으면 이미지 버튼(라벨 겹침), 없으면 색 버튼.
    private Button BuildActionButton(Transform parent, Font font, string text, Sprite sprite, Vector2 pos, Vector2 size, Color fallbackColor)
    {
        var img = CreateImage(parent, "Action", fallbackColor);
        Place(img.rectTransform, pos, size);

        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
        }

        var button = img.gameObject.AddComponent<Button>();
        button.targetGraphic = img;

        // 스프라이트에 이미 글자가 그려져 있으면 라벨은 생략(스프라이트가 없을 때만 텍스트).
        if (sprite == null)
        {
            var t = CreateText(img.transform, "Label", text, 40, font, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform);
            t.color = Color.white;
        }

        return button;
    }

    // ---------- 헬퍼 ----------

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

        if (f == null) f = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial", "돋움" }, 16);

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
