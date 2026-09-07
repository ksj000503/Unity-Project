using UnityEngine;

// 스폰 관련 참조를 Resources 에서 자동 주입하기 위한 설정 에셋.
// MonsterSpawner 의 bossPrefab 이 씬에서 비어 있을 때, Resources/SpawnConfig 의 값으로 채운다.
// → 씬 인스펙터 배선 없이 보스를 연결(로비/상점과 동일한 방식).
[CreateAssetMenu(fileName = "SpawnConfig", menuName = "Brotato/SpawnConfig")]
public class SpawnConfig : ScriptableObject
{
    [Tooltip("보스 프리팹. 스포너의 bossPrefab 이 비어 있으면 이 값이 자동 사용됨")]
    public GameObject bossPrefab;
}
