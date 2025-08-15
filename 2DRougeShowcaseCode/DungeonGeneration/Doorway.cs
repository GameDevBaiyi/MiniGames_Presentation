using UnityEngine;

namespace DungeonGunner.DungeonGeneration
{
    // 门的朝向；None 用于占位、不参与连接。
    public enum DoorwayOrientation
    {
        North,
        East,
        South,
        West,
        None
    }

    [System.Serializable]
    public sealed class Doorway
    {
        [SerializeField] private Vector2Int _position;
        [SerializeField] private DoorwayOrientation _orientation;
        [SerializeField] private GameObject _doorPrefab;

        public Vector2Int Position => _position;
        public DoorwayOrientation Orientation => _orientation;
        public GameObject DoorPrefab => _doorPrefab;

        // 生成过程中由 DungeonBuilder 直接翻转：一扇门一旦连接或判定无法使用就不会再被选中。
        public bool IsConnected { get; set; }
        public bool IsUnavailable { get; set; }

        // 供反序列化使用的无参构造。
        public Doorway()
        {
        }

        // 同一个 RoomTemplate 可能被用来生成多个房间实例，复制门配置但不带上一份的连接状态。
        public Doorway(Doorway source)
        {
            _position = source._position;
            _orientation = source._orientation;
            _doorPrefab = source._doorPrefab;
        }
    }
}
