using UnityEngine;
using System.Collections;

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

    [SerializeField] 
    private float spawnInterval;

    private Transform player;

    void Start()
    {
        if (monsterPrefab == null)
        {
            Debug.LogWarning("[MonsterSpawner] monsterPrefab이 비어있습니다.");

            return;
        }

        if (mapData == null)
        {
            Debug.LogWarning("[MonsterSpawner] mapData가 비어있습니다.");

            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj == null)
        {
            Debug.LogWarning("[MonsterSpawner] Player를 찾지 못했습니다");

            return;
        }
        player = playerObj.transform;

        StartCoroutine(SpawnRoutine());
    }

    private bool TryGetSpawnPosition(out Vector2 result)
    {
        float exclusionSqr = playerExclusionRadius * playerExclusionRadius;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            float x = Random.Range(mapData.minBounds.x, mapData.maxBounds.x);

            float y = Random.Range(mapData.minBounds.y, mapData.maxBounds.y);

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

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            SpawnMonster();
        }
    }
    private void SpawnMonster()
    {
        if (!TryGetSpawnPosition(out Vector2 spawnPos))
        {
            Debug.LogWarning("[MonsterSpawner] 스폰 위치를 찾지 못했습니다");

            return;
        }

        GameObject monster = ObjectPoolManager.Instance.Get(monsterPrefab);

        monster.transform.position = spawnPos;

        Health health = monster.GetComponent<Health>();

        if (health != null)
        {
            health.SetSourcePrefab(monsterPrefab);
        }
    }

}