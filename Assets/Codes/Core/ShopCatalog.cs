using System.Collections.Generic;
using UnityEngine;

// 상점이 제시할 무기·아이템 전체 목록. Assets/Resources/ShopCatalog.asset 로 두면
// ShopManager 가 Resources.Load 로 읽어 풀을 자동으로 채운다 → 씬 인스펙터 배선 불필요.
// 인스펙터에서 직접 넣은 풀이 있으면 그것과 합쳐지고, 중복은 제거된다.
[CreateAssetMenu(fileName = "ShopCatalog", menuName = "Brotato/ShopCatalog")]
public class ShopCatalog : ScriptableObject
{
    [Tooltip("상점 무기 후보 전체")]
    public List<WeaponData> weapons = new List<WeaponData>();

    [Tooltip("상점 아이템 후보 전체(등급별)")]
    public List<ItemData> items = new List<ItemData>();
}
