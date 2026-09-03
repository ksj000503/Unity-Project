using UnityEngine;

// 씬 전환에도 살아남는 정적 설정 홀더. 메인메뉴에서 고른 시작 무기를 게임 씬으로 넘긴다.
// ScriptableObject(WeaponData) 참조는 씬 언로드로 파괴되지 않으므로 정적 보관이 안전하다.
public static class GameConfig
{
    // 메인메뉴에서 선택한 시작 무기. null 이면(메뉴를 거치지 않고 바로 플레이) 지급하지 않음.
    public static WeaponData StartingWeapon;
}
