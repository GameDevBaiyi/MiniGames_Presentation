using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DungeonGunner.WeaponPooling
{
    // 开火节奏控制：蓄力、射速冷却、弹夹检查都在这里判定，真正的弹药生成交给对象池。
    // 原本用 Coroutine 实现连发，这里改写成 UniTaskVoid + CancellationToken：换武器或者
    // 组件销毁时，进行到一半的连发会被取消，不会继续往一把已经不再持有的武器上计数弹药。
    public sealed class FireWeapon : MonoBehaviour
    {
        [SerializeField] private PoolManager _poolManager;

        public event Action<Weapon> WeaponFired;
        public event Action<Weapon> ReloadRequested;

        private Weapon _activeWeapon;
        private Vector3 _shootPosition;
        private float _prechargeTimer;
        private float _cooldownTimer;
        private CancellationTokenSource _fireCancellationSource;

        private void OnDestroy()
        {
            _fireCancellationSource?.Cancel();
            _fireCancellationSource?.Dispose();
        }

        private void Update()
        {
            _cooldownTimer -= Time.deltaTime;
        }

        // 切换当前武器时调用；蓄力计时器按新武器的配置重置，并取消上一把武器还没打完的连发。
        public void SetActiveWeapon(Weapon weapon, Vector3 shootPosition)
        {
            _fireCancellationSource?.Cancel();
            _fireCancellationSource?.Dispose();
            _fireCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            _activeWeapon = weapon;
            _shootPosition = shootPosition;
            _prechargeTimer = weapon.Details.PrechargeTime;
        }

        // isFireHeld/wasFireHeldPreviousFrame 由输入层每帧传入；aimAngle 是玩家到鼠标的角度，
        // weaponAimAngle 是武器枪口到鼠标的角度——两者的取舍逻辑在 Ammo.Initialize 里。
        public void RequestFire(bool isFireHeld, bool wasFireHeldPreviousFrame, float aimAngle, float weaponAimAngle, Vector3 weaponAimDirection)
        {
            UpdatePrechargeTimer(wasFireHeldPreviousFrame);

            if (!isFireHeld || !IsReadyToFire())
            {
                return;
            }

            FireAsync(aimAngle, weaponAimAngle, weaponAimDirection, _fireCancellationSource.Token).Forget();

            _cooldownTimer = _activeWeapon.Details.FireRate;
            _prechargeTimer = _activeWeapon.Details.PrechargeTime;
        }

        private void UpdatePrechargeTimer(bool wasFireHeldPreviousFrame)
        {
            if (wasFireHeldPreviousFrame)
            {
                _prechargeTimer -= Time.deltaTime;
            }
            else
            {
                _prechargeTimer = _activeWeapon.Details.PrechargeTime;
            }
        }

        private bool IsReadyToFire()
        {
            if (_activeWeapon.TotalRemainingAmmo <= 0 && !_activeWeapon.Details.HasInfiniteAmmo)
            {
                return false;
            }

            if (_activeWeapon.IsReloading || _prechargeTimer > 0f || _cooldownTimer > 0f)
            {
                return false;
            }

            if (!_activeWeapon.Details.HasInfiniteClipCapacity && _activeWeapon.ClipRemainingAmmo <= 0)
            {
                ReloadRequested?.Invoke(_activeWeapon);
                return false;
            }

            return true;
        }

        // 一次开火可能连续生成多枚弹药（比如霰弹枪的散射），中间用 UniTask.Delay 隔开发射间隔。
        private async UniTaskVoid FireAsync(float aimAngle, float weaponAimAngle, Vector3 weaponAimDirection, CancellationToken cancellationToken)
        {
            AmmoDetailsSO ammoDetails = _activeWeapon.Details.AmmoDetails;

            if (ammoDetails == null || ammoDetails.AmmoPrefabs.Count == 0)
            {
                return;
            }

            int shotCount = UnityEngine.Random.Range(ammoDetails.SpawnAmountMin, ammoDetails.SpawnAmountMax + 1);
            float spawnInterval = shotCount > 1
                ? UnityEngine.Random.Range(ammoDetails.SpawnIntervalMin, ammoDetails.SpawnIntervalMax)
                : 0f;

            for (int i = 0; i < shotCount; i++)
            {
                SpawnAmmo(ammoDetails, aimAngle, weaponAimAngle, weaponAimDirection);

                if (spawnInterval > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(spawnInterval), cancellationToken: cancellationToken);
                }
            }

            if (!_activeWeapon.Details.HasInfiniteClipCapacity)
            {
                _activeWeapon.ClipRemainingAmmo--;
                _activeWeapon.TotalRemainingAmmo--;
            }

            WeaponFired?.Invoke(_activeWeapon);
        }

        private void SpawnAmmo(AmmoDetailsSO ammoDetails, float aimAngle, float weaponAimAngle, Vector3 weaponAimDirection)
        {
            Component prefab = ammoDetails.AmmoPrefabs[UnityEngine.Random.Range(0, ammoDetails.AmmoPrefabs.Count)];
            float speed = UnityEngine.Random.Range(ammoDetails.SpeedMin, ammoDetails.SpeedMax);

            Component instance = _poolManager.Get(prefab, _shootPosition, Quaternion.identity);
            IFireable ammo = (IFireable)instance;
            ammo.Initialize(ammoDetails, aimAngle, weaponAimAngle, speed, weaponAimDirection);
        }
    }
}
