using UnityEngine;

namespace DungeonGunner.AStarPathfinding
{
    // 单个寻路格子。GCost 初始为 int.MaxValue，代表"尚未被任何路径到达"；
    // FCost 相同时优先选 HCost 更小的（更接近终点）由调用方在入队优先级里体现，
    // Node 本身不再需要实现排序接口。
    public sealed class Node
    {
        public Vector2Int GridPosition { get; }
        public int GCost { get; set; } = int.MaxValue;
        public int HCost { get; set; }
        public Node ParentNode { get; set; }

        public int FCost => GCost + HCost;

        public Node(Vector2Int gridPosition)
        {
            GridPosition = gridPosition;
        }
    }
}
