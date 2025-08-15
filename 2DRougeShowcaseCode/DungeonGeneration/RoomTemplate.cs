using System.Collections.Generic;
using UnityEngine;

namespace DungeonGunner.DungeonGeneration
{
    // 房间的"蓝图"资产：外形 Prefab、局部坐标边界和自带的门列表。
    // 同一个 RoomNodeType 通常配置多份 RoomTemplate，生成时随机挑一份，地牢外观才不会每层都一样。
    [CreateAssetMenu(fileName = "RoomTemplate_", menuName = "Portfolio/2D Rogue/Room Template")]
    public sealed class RoomTemplate : ScriptableObject
    {
        [SerializeField] private RoomNodeType _roomNodeType;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Vector2Int _lowerBounds;
        [SerializeField] private Vector2Int _upperBounds;
        [SerializeField] private List<Doorway> _doorways = new List<Doorway>();

        public RoomNodeType RoomNodeType => _roomNodeType;
        public GameObject Prefab => _prefab;
        public Vector2Int LowerBounds => _lowerBounds;
        public Vector2Int UpperBounds => _upperBounds;
        public IReadOnlyList<Doorway> Doorways => _doorways;
    }
}
