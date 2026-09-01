using UnityEngine;
using UnityEngine.InputSystem;

// 웨이브/스테이지 진행 주체(싱글톤). 스폰을 직접 하지 않고 MonsterSpawner 를 제어한다.
// 웨이브 = 고정 시간(waveDuration) 경과 시에만 클리어 → 인터미션(상점) → StartNextWave 로 다음 스테이지.
// 스테이지 번호는 코인 가치·스폰 난이도 스케일의 기준(CurrentStage).
public class StageManager : MonoBehaviour
{
    public static StageManager Instance;

    [SerializeField] private MonsterSpawner spawner;

    [Header("웨이브")]
    [Tooltip("웨이브 지속 시간(초, 고정)")]
    [SerializeField] private float waveDuration = 20f;

    [Tooltip("시작 시 자동으로 1스테이지 웨이브 개시")]
    [SerializeField] private bool autoStart = true;

    [Header("디버그")]
    [Tooltip("임시: 인터미션 중 Space 로 다음 웨이브. 상점(ShopManager) 사용 시 꺼두세요.")]
    [SerializeField] private bool debugSpaceToContinue = false;

    private int currentStage = 1;
    private float timer;
    private bool waveActive;
    private bool intermission;

    public int CurrentStage => Mathf.Max(1, currentStage);
    public float TimeRemaining => Mathf.Max(0f, timer);
    public bool IsIntermission => intermission;

    public event System.Action<int> OnStageChanged;   // 새 스테이지 번호(웨이브 시작 시)
    public event System.Action<int> OnWaveCleared;    // 클리어된 스테이지 번호 → 상점 열기 신호
    public event System.Action<float> OnTimeChanged;  // 남은 시간(초)

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);

            return;
        }
    }

    private void Start()
    {
        if (spawner == null)
        {
            Debug.LogError("[StageManager] spawner 미할당 — 웨이브 진행 불가.", this);

            enabled = false;

            return;
        }

        if (autoStart) StartWave();
    }

    private void StartWave()
    {
        waveActive = true;

        intermission = false;

        timer = Mathf.Max(1f, waveDuration);

        OnStageChanged?.Invoke(CurrentStage);

        OnTimeChanged?.Invoke(TimeRemaining);

        spawner.BeginWave(CurrentStage);
    }

    private void Update()
    {
        if (waveActive)
        {
            timer -= Time.deltaTime;

            OnTimeChanged?.Invoke(TimeRemaining);

            if (timer <= 0f) ClearWave();

            return;
        }

        // 인터미션 중 임시 진행 키(상점 버튼 연결 전 테스트용).
        if (intermission && debugSpaceToContinue)
        {
            Keyboard kb = Keyboard.current;

            if (kb != null && kb.spaceKey.wasPressedThisFrame) StartNextWave();
        }
    }

    private void ClearWave()
    {
        if (!waveActive) return;

        waveActive = false;

        intermission = true;

        spawner.EndWave();

        OnWaveCleared?.Invoke(CurrentStage);
    }

    // 상점 "다시 시작" 버튼(또는 디버그 Space)에서 호출 → 다음 스테이지 웨이브 시작.
    public void StartNextWave()
    {
        if (!intermission) return;

        // 상점에서 일시정지했을 수 있으니 방어적으로 복구.
        Time.timeScale = 1f;

        currentStage++;

        StartWave();
    }

    public void SetStage(int stage)
    {
        currentStage = Mathf.Max(1, stage);
    }
}
