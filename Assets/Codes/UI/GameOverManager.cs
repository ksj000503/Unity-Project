using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

// 플레이어 사망 → 게임오버. 플레이어 Health.OnDied 구독 → 일시정지 + 게임오버 패널.
// "다시 시작" = 현재 씬 리로드(완전 초기화). UI는 런타임 자가 생성.
// 플레이어 Health 는 destroyOnDeath=false 여야 파괴되지 않고 여기로 넘어온다.
public class GameOverManager : MonoBehaviour
{
    [Header("참조 (비우면 Player 태그에서 탐색)")]
    [SerializeField] private Health playerHealth;

    [Header("UI (선택)")]
    [SerializeField] private Font uiFont;

    private GameObject root;
    private bool isGameOver;

    private void Awake()
    {
        BuildUI();

        SetVisible(false);
    }

    private void Start()
    {
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null) playerHealth = player.GetComponent<Health>();
        }

        if (playerHealth != null)
        {
            playerHealth.OnDied += HandlePlayerDied;
        }
        else
        {
            Debug.LogWarning("[GameOverManager] 플레이어 Health 없음 — 게임오버가 발동하지 않음. Player에 Health(destroyOnDeath=false) 추가 필요.", this);
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnDied -= HandlePlayerDied;
    }

    private void HandlePlayerDied(Health _)
    {
        if (isGameOver) return;

        isGameOver = true;

        SetVisible(true);

        Time.timeScale = 0f;
    }

    private void Restart()
    {
        Time.timeScale = 1f;

        Scene scene = SceneManager.GetActiveScene();

        SceneManager.LoadScene(scene.buildIndex);
    }

    private void SetVisible(bool on)
    {
        if (root != null) root.SetActive(on);
    }

    // ---------- UI 생성 ----------

    private void BuildUI()
    {
        Font font = ResolveFont();

        EnsureEventSystem();

        root = new GameObject("GameOverCanvas");
        root.transform.SetParent(transform, false);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300; // 상점(200)보다 위

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        var dim = CreateImage(root.transform, "Dim", new Color(0f, 0f, 0f, 0.8f));
        Stretch(dim.rectTransform);
        dim.raycastTarget = true;

        var title = CreateText(root.transform, "Title", "게임 오버", 90, font, TextAnchor.MiddleCenter);
        Place(title.rectTransform, new Vector2(0f, 80f), new Vector2(800f, 140f));
        title.color = new Color(0.95f, 0.3f, 0.3f);

        Button restart = CreateButton(root.transform, font, "다시 시작", new Vector2(0f, -80f), new Vector2(280f, 90f));
        restart.onClick.AddListener(Restart);
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
        if (uiFont != null) return uiFont;

        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");

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

    private Button CreateButton(Transform parent, Font font, string label, Vector2 pos, Vector2 size)
    {
        var img = CreateImage(parent, "Button", new Color(0.2f, 0.45f, 0.85f, 1f));
        Place(img.rectTransform, pos, size);

        var button = img.gameObject.AddComponent<Button>();
        button.targetGraphic = img;

        var text = CreateText(img.transform, "Label", label, 34, font, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);

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

    private void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
