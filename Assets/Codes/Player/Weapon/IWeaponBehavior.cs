using System.Collections;
using UnityEngine;

public interface IWeaponBehavior
{
    void Initialize(Weapon owner, WeaponData data);

    /// <summary>한 번의 공격을 수행하는 코루틴(근접이면 360도 스핀).</summary>
    IEnumerator Execute(Transform target);
}
