using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DungeonGunner.WeaponPooling
{
    // 装填计时。原本用 Coroutine 实现，这里改写成 UniTaskVoid + CancellationToken：
    // 换武器时会取消上一把武器还在跑的装填任务，避免装填完成事件打到已经不再持有的武器上。
    public sealed class ReloadWeapon : MonoBehaviour
    {
        public event Action<Weapon> WeaponReloaded;

        private CancellationTokenSource _reloadCancellationSource;

        private void OnDestroy()
        {
            _reloadCancellationSource?.Cancel();
            _reloadCancellationSource?.Dispose();
        }

        // topUpAmmoPercent 非 0 时代表从宝箱补给：先把总弹药按百分比加到上限，再决定弹夹能填多少。
        public void StartReload(Weapon weapon, int topUpAmmoPercent)
        {
            _reloadCancellationSource?.Cancel();
            _reloadCancellationSource?.Dispose();
            _reloadCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            ReloadAsync(weapon, topUpAmmoPercent, _reloadCancellationSource.Token).Forget();
        }

        // 切换到一把仍在装填中的武器时，从当前进度继续装填（weapon.ReloadTimer 不会被清零）。
        public void ResumeReloadIfInProgress(Weapon weapon)
        {
            if (weapon.IsReloading)
            {
                StartReload(weapon, 0);
            }
        }

        private async UniTaskVoid ReloadAsync(Weapon weapon, int topUpAmmoPercent, CancellationToken cancellationToken)
        {
            weapon.IsReloading = true;

            while (weapon.ReloadTimer < weapon.Details.ReloadTime)
            {
                weapon.ReloadTimer += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            if (topUpAmmoPercent != 0)
            {
                int ammoIncrease = Mathf.RoundToInt(weapon.Details.TotalAmmoCapacity * topUpAmmoPercent / 100f);
                weapon.TotalRemainingAmmo = Mathf.Min(weapon.TotalRemainingAmmo + ammoIncrease, weapon.Details.TotalAmmoCapacity);
            }

            bool canFillWholeClip = weapon.Details.HasInfiniteAmmo || weapon.TotalRemainingAmmo >= weapon.Details.ClipCapacity;
            weapon.ClipRemainingAmmo = canFillWholeClip ? weapon.Details.ClipCapacity : weapon.TotalRemainingAmmo;

            weapon.ReloadTimer = 0f;
            weapon.IsReloading = false;

            WeaponReloaded?.Invoke(weapon);
        }
    }
}
