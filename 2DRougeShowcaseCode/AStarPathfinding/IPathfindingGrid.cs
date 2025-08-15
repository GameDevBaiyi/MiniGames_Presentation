using UnityEngine;

namespace DungeonGunner.AStarPathfinding
{
    // AStar 只需要知道"这块网格长什么样"，不需要认识完整的房间/Tilemap 对象；
    // 真实项目里由房间的运行时组件实现这个接口，把网格代价数组包一层薄壳递给寻路算法。
    public interface IPathfindingGrid
    {
        Vector2Int LowerBounds { get; }
        Vector2Int UpperBounds { get; }
        Vector3 CellSize { get; }

        // 0 代表不可通行；大于 0 的值越大，A* 越倾向于绕开这一格（比如靠近墙角的格子）。
        int GetMovementPenalty(int localX, int localY);

        // 家具、箱子等可能挪动的动态障碍，独立于地图本身的移动代价数组维护。
        int GetObstaclePenalty(int localX, int localY);

        // localGridPosition 是相对房间左下角的局部坐标，返回值是格子中心的世界坐标。
        Vector3 GridToWorld(Vector2Int localGridPosition);
    }
}
