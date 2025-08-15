namespace DungeonGunner.WeaponPooling
{
    // 玩家/敌人持有的一把武器的运行时状态。WeaponDetailsSO 是不变的配置，
    // 这里的字段才是每次开火、装填都会变化的数据。
    public sealed class Weapon
    {
        public WeaponDetailsSO Details { get; }
        public int ClipRemainingAmmo { get; set; }
        public int TotalRemainingAmmo { get; set; }
        public float ReloadTimer { get; set; }
        public bool IsReloading { get; set; }

        public Weapon(WeaponDetailsSO details)
        {
            Details = details;
            ClipRemainingAmmo = details.ClipCapacity;
            TotalRemainingAmmo = details.TotalAmmoCapacity;
        }
    }
}
