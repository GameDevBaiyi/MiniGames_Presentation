using System.Collections.Generic;
using UnityEngine;

namespace DungeonGunner.DungeonGeneration
{
    // 一张地牢的"拓扑蓝图"：只描述节点之间的父子关系，不含具体房间外形。
    // 一个 DungeonLevel 通常配置多张节点图，生成时随机选一张，同一层楼的连通结构也会不一样。
    [CreateAssetMenu(fileName = "RoomNodeGraph_", menuName = "Portfolio/2D Rogue/Room Node Graph")]
    public sealed class RoomNodeGraphSO : ScriptableObject
    {
        [SerializeField] private List<RoomNode> _nodes = new List<RoomNode>();

        private readonly Dictionary<string, RoomNode> _nodesById = new Dictionary<string, RoomNode>();

        private void OnEnable()
        {
            RebuildIndex();
        }

        public RoomNode FindByType(RoomNodeType type)
        {
            foreach (RoomNode node in _nodes)
            {
                if (node.Type == type)
                {
                    return node;
                }
            }

            return null;
        }

        public RoomNode FindById(string id)
        {
            if (_nodesById.Count == 0)
            {
                RebuildIndex();
            }

            return _nodesById.TryGetValue(id, out RoomNode node) ? node : null;
        }

        public IEnumerable<RoomNode> GetChildren(RoomNode parent)
        {
            foreach (string childId in parent.ChildIds)
            {
                RoomNode child = FindById(childId);

                if (child != null)
                {
                    yield return child;
                }
            }
        }

        private void RebuildIndex()
        {
            _nodesById.Clear();

            foreach (RoomNode node in _nodes)
            {
                _nodesById[node.Id] = node;
            }
        }
    }
}
