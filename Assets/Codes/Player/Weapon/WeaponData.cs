using UnityEngine;


[CreateAssetMenu(fileName = "WeaponData", menuName = "Brotato/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("식별")]
    public string weaponName = "New Weapon";
    public Sprite icon;

    [Tooltip("Weapon 컴포넌트를 가진 무기 프리팹")]
    public GameObject weaponPrefab;

    [Header("공통 전투")]
    public int damage = 10;

    [Tooltip("공격 1회 후 재공격까지 대기(초)")]
    public float attackCooldown = 1f;

    [Tooltip("적을 탐지해 공격을 시작하는 반경")]
    public float detectRange = 2.5f;

    [Header("근접 스핀")]
    [Tooltip("360도 도는 데 걸리는 시간(초)")]
    public float spinDuration = 0.3f;

    [Tooltip("스핀 중 피해를 주는 반경")]
    public float spinRadius = 1.2f;
}