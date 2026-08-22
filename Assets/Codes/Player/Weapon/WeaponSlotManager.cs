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
    // 같은 무기(동일 WeaponData 참조)를 이미 장착 중이면 슬롯을 늘리지 않고 레벨업.
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

        // 중복 장비 → 레벨업 (슬롯이 가득 차 있어도 강화는 가능)
        Weapon existing = FindSlotWithData(data);

        if (existing != null)
        {
            existing.LevelUp();

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
