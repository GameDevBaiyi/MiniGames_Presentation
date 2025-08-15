using UnityEngine;

namespace DungeonGunner.WeaponPooling
{
    // 从对象池取出的弹药预制体需要实现的最小接口；FireWeapon 只认这个接口，
    // 不关心具体是普通子弹还是会继续生成子弹药的弹幕图案。
    public interface IFireable
    {
        void Initialize(AmmoDetailsSO details, float aimAngle, float weaponAimAngle, float speed, Vector3 weaponAimDirection);

        GameObject GetGameObject();
    }
}
