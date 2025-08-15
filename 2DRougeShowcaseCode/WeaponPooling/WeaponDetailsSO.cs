using UnityEngine;

namespace DungeonGunner.WeaponPooling
{
    // 一把武器的静态配置：外观、开火节奏和弹药规则都在这里，运行时状态在 Weapon 里。
    [CreateAssetMenu(fileName = "WeaponDetails_", menuName = "Portfolio/2D Rogue/Weapon Details")]
    public sealed class WeaponDetailsSO : ScriptableObject
    {
        [Header("外观")]
        [SerializeField] private string _weaponName = string.Empty;
        [SerializeField] private Sprite _weaponSprite;
        [SerializeField] private AmmoDetailsSO _ammoDetails;

        [Header("开火节奏")]
        [Tooltip("两次射击的最小间隔（秒）")]
        [Min(0f)]
        [SerializeField] private float _fireRate = 0.2f;
        [Tooltip("按住开火键蓄力所需时间（秒），0 表示不需要蓄力")]
        [Min(0f)]
        [SerializeField] private float _prechargeTime;
        [Min(0f)]
        [SerializeField] private float _reloadTime = 1f;

        [Header("弹药规则")]
        [SerializeField] private bool _hasInfiniteAmmo;
        [SerializeField] private bool _hasInfiniteClipCapacity;
        [Min(1)]
        [SerializeField] private int _clipCapacity = 6;
        [Min(1)]
        [SerializeField] private int _totalAmmoCapacity = 100;

        public string WeaponName => _weaponName;
        public Sprite WeaponSprite => _weaponSprite;
        public AmmoDetailsSO AmmoDetails => _ammoDetails;
        public float FireRate => _fireRate;
        public float PrechargeTime => _prechargeTime;
        public float ReloadTime => _reloadTime;
        public bool HasInfiniteAmmo => _hasInfiniteAmmo;
        public bool HasInfiniteClipCapacity => _hasInfiniteClipCapacity;
        public int ClipCapacity => _clipCapacity;
        public int TotalAmmoCapacity => _totalAmmoCapacity;
    }
}
