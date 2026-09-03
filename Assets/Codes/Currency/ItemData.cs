using System.Collections.Generic;
using UnityEngine;

// 상점에서 구매하는 강화 아이템. 등급(행운 뽑기)과 효과 리스트를 가진 ScriptableObject.
// 구매 시 ShopManager 가 effects 를 PlayerStats 에 적용한다.

public enum ItemRarity { Normal, Epic, Unique }

// 아이템이 올리는 스탯 종류.
public enum PlayerStatType
{
    DamageUp,      // 무기 데미지 +N%
    CooldownDown,  // 공격 쿨다운 -N%
    MaxHpUp,       // 최대 HP +N (구매 시 현재 HP도 +N 회복)
    LuckUp,        // 행운 +N (상점 상위 등급 확률 증가)
}

[System.Serializable]
public struct StatModifier
{
    public PlayerStatType stat;
    public float amount;
}

[CreateAssetMenu(fileName = "ItemData", menuName = "Brotato/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName = "New Item";
    public ItemRarity rarity = ItemRarity.Normal;
    public Sprite icon;
    [TextArea] public string description;
    public List<StatModifier> effects = new List<StatModifier>();
    public int price = 8;
}
