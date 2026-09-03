using UnityEngine;

public class WeaponSlotManager : MonoBehaviour
{
    [Header("슬롯 배치")]
    [SerializeField]
    private int slotCount = 6;

    [SerializeField]
    private float slotRadius = 0.8f;

    [Header("공격 대상 레이어")]
    [SerializeField]
    private LayerMask enemyMask;

    private Transform[] anchors;

    private Weapon[] slots;

    // 장착/레벨업 등 슬롯 구성이 바뀌면 발행. HUD 무기 아이콘 바가 구독해 갱신.
    public event System.Action OnWeaponsChanged;

    // HUD 가 슬롯을 훑기 위한 읽기 접근자.
    public int SlotCount => slotCount;

    public Weapon GetSlot(int index)
    {
        if (slots == null || index < 0 || index >= slots.Length) return null;

        return slots[index];
    }

    private void Awake()
    {
        slotCount = Mathf.Max(1, slotCount);

        anchors = new Transform[slotCount];

        slots = new Weapon[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            float rad = (360f / slotCount * i) * Mathf.Deg2Rad;

            Vector3 localPos = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * slotRadius;

            var anchor = new GameObject($"WeaponAnchor_{i}").transform;

            anchor.SetParent(transform, false);

            anchor.localPosition = localPos;

            anchors[i] = anchor;
        }
    }

    // 무기 획득 진입점.
    // 같은 무기(동일 WeaponData 참조)를 이미 장착 중이면 슬롯을 늘리지 않고 레벨업(최대 레벨이면 실패).
    // 아니면 1번 슬롯부터 차례로 빈 칸에 장착. 반환값 true = 장착 또는 레벨업 성공.
    public bool AddWeapon(WeaponData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[WeaponSlotManager] null WeaponData — 장착 무시.", this);

            return false;
        }
        if (data.weaponPrefab == null)
        {
            Debug.LogError($"[WeaponSlotManager] {data.name}: weaponPrefab 미할당.", this);

            return false;
        }

        // 중복 장비 → 레벨업 (슬롯이 가득 차 있어도 강화는 가능). 단, 최대 레벨이면 실패 → 상점이 환불.
        Weapon existing = FindSlotWithData(data);

        if (existing != null)
        {
            if (!existing.CanLevelUp)
            {
                Debug.Log($"[WeaponSlotManager] {data.name} 최대 레벨(Lv{existing.MaxLevel}) — 강화 불가.", this);

                return false;
            }

            existing.LevelUp();

            OnWeaponsChanged?.Invoke();

            return true;
        }

        int idx = FirstEmptySlot();

        if (idx < 0)
        {
            Debug.Log("[WeaponSlotManager] 모든 슬롯이 가득 참 — 장착 무시.", this);

            return false;
        }

        var go = Instantiate(data.weaponPrefab, anchors[idx].position, anchors[idx].rotation, anchors[idx]);

        var weapon = go.GetComponent<Weapon>();

        if (weapon == null)
        {
            Debug.LogError($"[WeaponSlotManager] {data.name}.weaponPrefab 에 Weapon 컴포넌트 없음.", this);

            Destroy(go);

            return false;
        }

        weapon.Setup(data, enemyMask);

        slots[idx] = weapon;

        OnWeaponsChanged?.Invoke();

        return true;
    }

    // 동일 WeaponData 를 장착 중인 슬롯의 Weapon 반환(없으면 null).
    private Weapon FindSlotWithData(WeaponData data)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].Data == data) return slots[i];
        }

        return null;
    }

    private int FirstEmptySlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) return i;
        }

        return -1;
    }
}
