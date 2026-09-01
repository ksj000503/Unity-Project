using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 몬스터 스폰. 웨이브 단위로 StageManager 가 BeginWave/EndWave 로 제어한다.
// 스테이지가 오를수록 스폰 간격이 짧아지고 몬스터 HP가 조금씩 증가.
// 활성 몬스터가 전멸하면 OnAllMonstersCleared 발행(웨이브 조기 클리어용).
public class MonsterSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject monsterPrefab;

    [SerializeField]
    private MapData mapData;

    [SerializeField]
    private float playerExclusionRadius;

    [SerializeField]
    private int maxSpawnAttempts = 10;

    [Header("스폰 간격 (스테이지 스케일)")]
    [SerializeField] private float baseSpawnInterval = 1.5f;
    [Tooltip("스테이지당 스폰 간격 감소량(초)")]
    [SerializeField] private float spawnIntervalReducePerStage = 0.1f;
    [SerializeField] private float minSpawnInterval = 0.3f;

    [Header("몬스터 HP (스테이지 스케일)")]
    [Tooltip("스테이지당 최대 HP 증가율 (0.15 = +15%)")]
    [SerializeField] private float hpGrowthPerStage = 0.15f;

    // 활성 몬스터가 모두 죽었을 때(전멸) 발행. StageManager 가 구독해 웨이브 조기 클리어.
    public event System.Action OnAllMonstersCleared;

    private Transform player;
    private int baseMonsterHp = 1;
    private Camera cam;

    private bool spawning;
    private bool hasSpawnedThisWave;
    private int currentStage = 1;
    private Coroutine spawnRoutine;

    private readonly HashSet<Health> active = new HashSet<Health>();

    // Awake 에서 참조를 잡아야 StageManager.Start 의 BeginWave 호출 시점에 player 가 준비됨(실행 순서 안전).
    void Awake()
    {
        if (monsterPrefab == null)
        {
            Debug.LogWarning("[MonsterSpawner] monsterPrefab 이 비어있습니다.");

            return;
        }

        if (mapData == null)
        {
            Debug.LogWarning("[MonsterSpawner] mapData 가 비어있습니다.");

            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj == null)
        {
            Debug.LogWarning("[MonsterSpawner] Player 를 찾지 못했습니다.");

            return;
        }
        player = playerObj.transform;

        // 프리팹의 기준 HP 를 스테이지 스케일 기준값으로 캐시(인스턴스 필드가 덮여도 원본 유지).
        Health prefabHealth = monsterPrefab.GetComponent<Health>();

        if (prefabHealth != null)
        {
            baseMonsterHp = Mathf.Max(1, prefabHealth.MaxHp());
        }

        // 같은 몬스터 레이어끼리는 충돌·트리거 판정을 끔 → 몬스터끼리 서로 때리는(피아) 피해 원천 차단.
        Physics2D.IgnoreLayerCollision(monsterPrefab.layer, monsterPrefab.layer, true);
    }

    // 웨이브 시작: 해당 스테이지 난이도로 스폰 개시.
    public void BeginWave(int stage)
    {
        if (player == null || monsterPrefab == null || mapData == null) return;

        currentStage = Mathf.Max(1, stage);

        hasSpawnedThisWave = false;

        spawning = true;

        if (spawnRoutine != null) StopCoroutine(spawnRoutine);

        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    // 웨이브 종료: 스폰 중단 + 남은 몬스터 디스폰(풀 복귀, 사망 처리 아님 → 코인 드랍 없음).
    public void EndWave()
    {
        spawning = false;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);

            spawnRoutine = null;
        }

        DespawnAll();
    }

    // 씬 리로드/오브젝트 파괴 시 스폰 코루틴이 사라진 참조를 건드리지 않도록 즉시 정지.
    private void OnDisable()
    {
        spawning = false;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);

            spawnRoutine = null;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        float interval = Mathf.Max(minSpawnInterval, baseSpawnInterval - spawnIntervalReducePerStage * (currentStage - 1));

        while (spawning)
        {
            yield return new WaitForSeconds(interval);

            if (!spawning) yield break;

            SpawnMonster();
        }
    }

    private void SpawnMonster()
    {
        if (monsterPrefab == null) return;

        if (!TryGetSpawnPosition(out Vector2 spawnPos))
        {
            Debug.LogWarning("[MonsterSpawner] 스폰 위치를 찾지 못했습니다.");

            return;
        }

        GameObject monster = ObjectPoolManager.Instance.Get(monsterPrefab);

        if (monster == null) return;

        monster.transform.position = spawnPos;

        Health health = monster.GetComponent<Health>();

        if (health != null)
        {
            health.SetSourcePrefab(monsterPrefab);

            // 스테이지 스케일 HP 적용(Get 직후 OnEnable 리셋 이후에 덮어씀).
            int scaledHp = Mathf.RoundToInt(baseMonsterHp * (1f + hpGrowthPerStage * (currentStage - 1)));

            health.SetMax(Mathf.Max(1, scaledHp));

            active.Add(health);

            health.OnDied += HandleMonsterDied;
        }

        hasSpawnedThisWave = true;
    }

    private void HandleMonsterDied(Health h)
    {
        active.Remove(h);

        h.OnDied -= HandleMonsterDied;

        // 스폰이 한 번이라도 있었고 전부 죽었으면 전멸 → 웨이브 조기 클리어.
        if (spawning && hasSpawnedThisWave && active.Count == 0)
        {
            OnAllMonstersCleared?.Invoke();
        }
    }

    private void DespawnAll()
    {
        foreach (var h in active)
        {
            if (h == null) continue;

            h.OnDied -= HandleMonsterDied;

            if (h.gameObject.activeSelf)
            {
                ObjectPoolManager.Instance.Return(monsterPrefab, h.gameObject);
            }
        }

        active.Clear();
    }

    private bool TryGetSpawnPosition(out Vector2 result)
    {
        float exclusionSqr = playerExclusionRadius * playerExclusionRadius;

        // 스폰 영역 = 카메라 화면 안(살짝 안쪽). 카메라 없으면 MapData 로 폴백.
        float minX, maxX, minY, maxY;

        if (cam == null) cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();

        if (cam != null && cam.orthographic)
        {
            float inset = 0.92f;
            float halfH = cam.orthographicSize * inset;
            float halfW = cam.orthographicSize * cam.aspect * inset;
            Vector2 center = cam.transform.position;
            minX = center.x - halfW; maxX = center.x + halfW;
            minY = center.y - halfH; maxY = center.y + halfH;
        }
        else
        {
            minX = mapData.minBounds.x; maxX = mapData.maxBounds.x;
            minY = mapData.minBounds.y; maxY = mapData.maxBounds.y;
        }

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            float x = Random.Range(minX, maxX);

            float y = Random.Range(minY, maxY);

            Vector2 candidate = new Vector2(x, y);

            float sqrDistance = (candidate - (Vector2)player.position).sqrMagnitude;

            if (sqrDistance >= exclusionSqr)
            {
                result = candidate;

                return true;
            }
        }
        result = Vector2.zero;

        return false;
    }
}
