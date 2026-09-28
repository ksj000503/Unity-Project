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

    // 플레이어 사망 후에는 웨이브를 더 진행하지 않는다.
    // 같은 프레임에 사망(물리 단계)과 타이머 종료(Update)가 겹쳐도 상점이 열리지 않게 하기 위함.
    private Health playerHealth;
    private bool playerDead;

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

        SubscribePlayerDeath();

        if (autoStart) StartWave();
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnDied -= HandlePlayerDied;
    }

    private void SubscribePlayerDeath()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null) playerHealth = player.GetComponent<Health>();

        if (playerHealth != null)
        {
            playerHealth.OnDied += HandlePlayerDied;
        }
        else
        {
            Debug.LogWarning("[StageManager] 플레이어 Health 없음 — 사망 시 웨이브 중단이 동작하지 않음.", this);
        }
    }

    private void HandlePlayerDied(Health _)
    {
        playerDead = true;

        waveActive = false;

        intermission = false;
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

    // 디버그 콘솔용: 진행/인터미션 무관하게 지정 스테이지 웨이브를 새로 시작.
    // 보스 주기에 걸리는 스테이지로 점프하면 그 즉시 보스가 등장한다.
    public void DebugJumpToStage(int stage)
    {
        if (spawner == null || playerDead) return;

        Time.timeScale = 1f;

        spawner.EndWave();               // 현재 몬스터 정리 + 스폰 중단

        currentStage = Mathf.Max(1, stage);

        StartWave();                     // 해당 스테이지로 재시작
    }

    // 디버그 콘솔용: 타이머를 기다리지 않고 지금 웨이브를 끝낸다.
    // 실제 시간 종료와 같은 ClearWave 경로를 타므로 가드(사망 후 무시 등)도 그대로 검증된다.
    public void DebugEndWaveNow()
    {
        ClearWave();
    }
}
