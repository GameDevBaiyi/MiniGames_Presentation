using System.Collections.Generic;
using UnityEngine;

namespace DungeonGunner.WeaponPooling
{
    // 一种弹药的配置：外观、伤害、散布和拖尾都在这里，一把武器可以换弹药类型而不用换代码逻辑。
    [CreateAssetMenu(fileName = "AmmoDetails_", menuName = "Portfolio/2D Rogue/Ammo Details")]
    public sealed class AmmoDetailsSO : ScriptableObject
    {
        [Header("外观与预制体")]
        [SerializeField] private Sprite _ammoSprite;
        [Tooltip("多个预制体时随机挑一个，可以是普通弹药，也可以是会继续生成子弹药的弹幕图案；" +
            "字段类型是 Component 而不是 GameObject，是为了让 PoolManager 能直接按这个引用管理对象池")]
        [SerializeField] private Component[] _ammoPrefabs = System.Array.Empty<Component>();
        [SerializeField] private Material _material;
        [Tooltip("蓄力型武器（比如激光）发射前的停留材质")]
        [SerializeField] private Material _chargeMaterial;
        [Min(0f)]
        [SerializeField] private float _chargeTime;

        [Header("基础参数")]
        [Min(0)]
        [SerializeField] private int _damage = 1;
        [Min(0f)]
        [SerializeField] private float _speedMin = 20f;
        [Min(0f)]
        [SerializeField] private float _speedMax = 20f;
        [Min(0f)]
        [SerializeField] private float _range = 20f;

        [Header("散布")]
        [Tooltip("命中精度的随机偏移角度范围，数值越大越不准")]
        [SerializeField] private float _spreadMin;
        [SerializeField] private float _spreadMax;

        [Header("单次开火的弹药数量")]
        [Min(1)]
        [SerializeField] private int _spawnAmountMin = 1;
        [Min(1)]
        [SerializeField] private int _spawnAmountMax = 1;
        [Min(0f)]
        [SerializeField] private float _spawnIntervalMin;
        [Min(0f)]
        [SerializeField] private float _spawnIntervalMax;

        [Header("拖尾")]
        [SerializeField] private bool _hasTrail;
        [Min(0f)]
        [SerializeField] private float _trailTime = 3f;
        [SerializeField] private Material _trailMaterial;
        [Range(0f, 1f)] [SerializeField] private float _trailStartWidth;
        [Range(0f, 1f)] [SerializeField] private float _trailEndWidth;

        public Sprite AmmoSprite => _ammoSprite;
        public IReadOnlyList<Component> AmmoPrefabs => _ammoPrefabs;
        public Material Material => _material;
        public Material ChargeMaterial => _chargeMaterial;
        public float ChargeTime => _chargeTime;
        public int Damage => _damage;
        public float SpeedMin => _speedMin;
        public float SpeedMax => _speedMax;
        public float Range => _range;
        public float SpreadMin => _spreadMin;
        public float SpreadMax => _spreadMax;
        public int SpawnAmountMin => _spawnAmountMin;
        public int SpawnAmountMax => _spawnAmountMax;
        public float SpawnIntervalMin => _spawnIntervalMin;
        public float SpawnIntervalMax => _spawnIntervalMax;
        public bool HasTrail => _hasTrail;
        public float TrailTime => _trailTime;
        public Material TrailMaterial => _trailMaterial;
        public float TrailStartWidth => _trailStartWidth;
        public float TrailEndWidth => _trailEndWidth;
    }
}
