using System.Collections.Generic;
using UnityEngine;

// 메인메뉴가 런타임 UI 를 구성할 때 참조하는 에셋 묶음.
// Assets/Resources/LobbyConfig.asset 로 두면 MainMenu 가 Resources.Load 로 읽는다 → 씬 배선 불필요.
[CreateAssetMenu(fileName = "LobbyConfig", menuName = "Brotato/LobbyConfig")]
public class LobbyConfig : ScriptableObject
{
    [Header("타이틀")]
    [Tooltip("타이틀 로고 텍스처(RawImage 로 통짜 표시). 비우면 텍스트 타이틀 사용")]
    public Texture titleTexture;

    [Header("버튼 스프라이트 (비우면 색 버튼)")]
    public Sprite startSprite;
    public Sprite quitSprite;

    [Header("시작 무기 후보")]
    [Tooltip("메인메뉴에서 선택지로 노출할 무기들")]
    public List<WeaponData> startingWeapons = new List<WeaponData>();
}
