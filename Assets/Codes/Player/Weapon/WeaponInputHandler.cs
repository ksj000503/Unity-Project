using UnityEngine;
using UnityEngine.InputSystem;

// 디버그용 무기 지급 입력. 상점(P4) 구현 시 제거 예정.
// 키 1 = 근접 무기, 키 2 = 원거리 무기 획득 요청 → WeaponSlotManager 로 위임.
// 이 프로젝트는 신규 Input System 을 쓰므로 옛 Input.GetKeyDown 대신 Keyboard.current 사용.
[RequireComponent(typeof(WeaponSlotManager))]
public class WeaponInputHandler : MonoBehaviour
{
    [Header("지급할 무기 데이터 (디버그)")]
    [Tooltip("키 1 로 획득")]
    [SerializeField] private WeaponData meleeWeapon;

    [Tooltip("키 2 로 획득")]
    [SerializeField] private WeaponData rangedWeapon;

    private WeaponSlotManager slotManager;

    private void Awake()
    {
        slotManager = GetComponent<WeaponSlotManager>();

        if (slotManager == null)
        {
            Debug.LogError("[WeaponInputHandler] 같은 오브젝트에 WeaponSlotManager 가 없음.", this);

            enabled = false;
        }
    }

    private void Update()
    {
        // 키보드 디바이스가 없으면(패드 전용 등) 조용히 무시.
        Keyboard kb = Keyboard.current;

        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) TryEquip(meleeWeapon, "근접");

        if (kb.digit2Key.wasPressedThisFrame) TryEquip(rangedWeapon, "원거리");
    }

    private void TryEquip(WeaponData data, string label)
    {
        if (data == null)
        {
            Debug.LogWarning($"[WeaponInputHandler] {label} 무기 WeaponData 미할당 — 입력 무시.", this);

            return;
        }

        slotManager.AddWeapon(data);
    }
}
