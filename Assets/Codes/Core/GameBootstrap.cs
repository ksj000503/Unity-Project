using UnityEngine;
using UnityEngine.SceneManagement;

// 게임 씬 진입 시 메인메뉴에서 고른 시작 무기를 플레이어에게 1개 지급한다.
// 씬에 아무것도 배치할 필요 없음: 런타임 초기화 훅으로 sceneLoaded 를 한 번만 구독.
// GameConfig.StartingWeapon 이 null 이면(메뉴 미경유·직접 플레이) 아무 것도 하지 않아 기존 흐름을 유지.
public static class GameBootstrap
{
    // 시작 무기를 지급할 게임 씬 이름(메인메뉴 씬에서는 지급 안 함).
    private const string GameSceneName = "SampleScene";

    private static bool hooked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (hooked) return;

        hooked = true;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != GameSceneName) return;

        WeaponData weapon = GameConfig.StartingWeapon;

        if (weapon == null) return; // 메뉴를 거치지 않았으면 지급 없음(디버그 키로 획득 가능)

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("[GameBootstrap] Player 태그 오브젝트 없음 — 시작 무기 미지급.");

            return;
        }

        WeaponSlotManager slots = player.GetComponent<WeaponSlotManager>();

        if (slots == null)
        {
            Debug.LogWarning("[GameBootstrap] Player 에 WeaponSlotManager 없음 — 시작 무기 미지급.");

            return;
        }

        slots.AddWeapon(weapon);
    }
}
