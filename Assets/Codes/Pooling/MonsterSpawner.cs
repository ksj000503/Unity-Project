using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 몬스터 스폰. 웨이브 단위로 StageManager 가 BeginWave/EndWave 로 제어한다.
// 스테이지가 오를수록 스폰 간격이 짧아지고 몬스터 HP가 조금씩 증가.
// 활성 몬스터가 전멸하면 OnAllMonstersCleared 발행(웨이브 조기 클리어용).
//
// [다중 몬스터] monsterTable(가중치·최소스테이지)에서 뽑아 스폰. 비어 있으면 monsterPrefab 폴백.
// [보스] bossPrefab 이 있고 stage % bossEveryStages == 0 이면 웨이브 시작 시 보스 1마리 추가 스폰.
public class MonsterSpawner : MonoBehaviour
{
    // 일반 몬스터 후보 1종. weight=상대 출현 확률, minStage=이 스테이지부터 등장.
    [System.Serializable]
    public class SpawnEntry
    {
        public GameObject prefab;
        public float weight = 1f;
        public int minStage = 1;
    }

    [Header("일반 몬스터 (가중치 추첨)")]
    [Tooltip("여러 종류를 가중치로 추첨. 비어 있으면 아래 monsterPrefab 을 사용.")]
    [SerializeField] private List<SpawnEntry> monsterTable = new List<SpawnEntry>();

    [Tooltip("폴백/기본 몬스터(monsterTable 이 비었을 때 사용). 하위호환용.")]
    [SerializeField] private GameObject monsterPrefab;

    [Header("보스")]
    [Tooltip("보스 프리팹(없으면 보스 미등장).")]
    [SerializeField] private GameObject bossPrefab;
    [Tooltip("몇 스테이지마다 보스가 등장하는지(예: 5 → 5,10,15…). 0 이면 등장 안 함.")]
    [SerializeField] private int bossEveryStages = 5;

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
    private Camera cam;

    private bool spawning;
    private bool hasSpawnedThisWave;
    private int currentStage = 1;
    private Coroutine spawnRoutine;

    private readonly HashSet<Health> active = new HashSet<Health>();
    // 각 활성 몬스터가 어느 프리팹 풀에서 나왔는지 → 디스폰 시 올바른 풀로 반환.
    private readonly Dictionary<Health, GameObject> sourceOf = new Dictionary<Health, GameObject>();
    // 프리팹별 기준 HP 캐시(원본 유지, 매 스폰 GetComponent 회피).
    private readonly Dictionary<GameObject, int> baseHpOf = new Dictionary<GameObject, int>();

    // Awake 에서 참조를 잡아야 StageManager.Start 의 BeginWave 호출 시점에 player 가 준비됨(실행 순서 안전).
    void Awake()
    {
        if (!HasAnySpawnable())
        {
            Debug.LogWarning("[MonsterSpawner] 스폰 가능한 몬스터가 없습니다(monsterTable/monsterPrefab/bossPrefab 모두 비어있음).");

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

        // 같은 몬스터 레이어끼리는 충돌·트리거 판정을 끔 → 몬스터끼리 서로 때리는(피아) 피해 원천 차단.
        GameObject sample = FirstSpawnable();

        if (sample != null)
        {
            Physics2D.IgnoreLayerCollision(sample.layer, sample.layer, true);
        }
    }

    // 웨이브 시작: 해당 스테이지 난이도로 스폰 개시. 보스 스테이지면 보스 1마리 즉시 스폰.
    public void BeginWave(int stage)
    {
        if (player == null || mapData == null || !HasAnySpawnable()) return;

        currentStage = Mathf.Max(1, stage);

        hasSpawnedThisWave = false;

        spawning = true;

        // 보스 등장 조건: bossPrefab 존재 + 주기 일치.
        if (bossPrefab != null && bossEveryStages > 0 && currentStage % bossEveryStages == 0)
        {
            SpawnOne(bossPrefab);
        }

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
        SpawnOne(PickPrefab());
    }

    // 실제 스폰 1마리. prefab 별 기준 HP 를 스테이지 스케일로 덮어씀. 소스 프리팹 추적.
    private void SpawnOne(GameObject prefab)
    {
        if (prefab == null) return;

        if (!TryGetSpawnPosition(out Vector2 spawnPos))
        {
            Debug.LogWarning("[MonsterSpawner] 스폰 위치를 찾지 못했습니다.");

            return;
        }

        GameObject monster = ObjectPoolManager.Instance.Get(prefab);

        if (monster == null) return;

        monster.transform.position = spawnPos;

        Health health = monster.GetComponent<Health>();

        if (health != null)
        {
            health.SetSourcePrefab(prefab);

            // 스테이지 스케일 HP 적용(Get 직후 OnEnable 리셋 이후에 덮어씀).
            int scaledHp = Mathf.RoundToInt(BaseHpOf(prefab) * (1f + hpGrowthPerStage * (currentStage - 1)));

            health.SetMax(Mathf.Max(1, scaledHp));

            active.Add(health);

            sourceOf[health] = prefab;

            health.OnDied += HandleMonsterDied;
        }

        hasSpawnedThisWave = true;
    }

    // monsterTable 에서 (스테이지 조건 통과분) 가중치 추첨. 비면 monsterPrefab 폴백.
    private GameObject PickPrefab()
    {
        float total = 0f;

        for (int i = 0; i < monsterTable.Count; i++)
        {
            SpawnEntry e = monsterTable[i];

            if (e != null && e.prefab != null && currentStage >= e.minStage)
            {
                total += Mathf.Max(0f, e.weight);
            }
        }

        if (total <= 0f) return monsterPrefab;

        float r = Random.value * total;

        for (int i = 0; i < monsterTable.Count; i++)
        {
            SpawnEntry e = monsterTable[i];

            if (e == null || e.prefab == null || currentStage < e.minStage) continue;

            r -= Mathf.Max(0f, e.weight);

            if (r <= 0f) return e.prefab;
        }

        return monsterPrefab;
    }

    private int BaseHpOf(GameObject prefab)
    {
        if (prefab == null) return 1;

        if (baseHpOf.TryGetValue(prefab, out int cached)) return cached;

        int hp = 1;

        Health ph = prefab.GetComponent<Health>();

        if (ph != null) hp = Mathf.Max(1, ph.MaxHp());

        baseHpOf[prefab] = hp;

        return hp;
    }

    private void HandleMonsterDied(Health h)
    {
        active.Remove(h);

        sourceOf.Remove(h);

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
                GameObject src = sourceOf.TryGetValue(h, out GameObject p) ? p : monsterPrefab;

                ObjectPoolManager.Instance.Return(src, h.gameObject);
            }
        }

        active.Clear();

        sourceOf.Clear();
    }

    private bool HasAnySpawnable()
    {
        if (monsterPrefab != null) return true;

        if (bossPrefab != null) return true;

        for (int i = 0; i < monsterTable.Count; i++)
        {
            if (monsterTable[i] != null && monsterTable[i].prefab != null) return true;
        }

        return false;
    }

    private GameObject FirstSpawnable()
    {
        for (int i = 0; i < monsterTable.Count; i++)
        {
            if (monsterTable[i] != null && monsterTable[i].prefab != null) return monsterTable[i].prefab;
        }

        if (monsterPrefab != null) return monsterPrefab;

        return bossPrefab;
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
