using System.Collections.Generic;
using UnityEngine;

namespace DungeonGunner.AStarPathfinding
{
    // 基于房间局部网格代价的 A*。
    public static class AStar
    {
        // 找不到路径时返回 null；找到时返回从起点到终点、按世界坐标排列的移动栈（栈顶是下一步该走的格子）。
        public static Stack<Vector3> BuildPath(IPathfindingGrid grid, Vector3Int startGridPosition, Vector3Int endGridPosition)
        {
            Vector2Int lowerBounds = grid.LowerBounds;
            Vector2Int upperBounds = grid.UpperBounds;

            Vector3Int localStart = startGridPosition - (Vector3Int)lowerBounds;
            Vector3Int localEnd = endGridPosition - (Vector3Int)lowerBounds;

            GridNodes gridNodes = new GridNodes(upperBounds.x - lowerBounds.x + 1, upperBounds.y - lowerBounds.y + 1);

            Node startNode = gridNodes.GetNode(localStart.x, localStart.y);
            Node targetNode = gridNodes.GetNode(localEnd.x, localEnd.y);

            Node endPathNode = FindShortestPath(startNode, targetNode, gridNodes, grid);

            return endPathNode != null ? CreatePathStack(endPathNode, grid) : null;
        }

        private static Node FindShortestPath(Node startNode, Node targetNode, GridNodes gridNodes, IPathfindingGrid grid)
        {
            PriorityQueue<Node, (int FCost, int HCost)> openQueue = new PriorityQueue<Node, (int, int)>();
            HashSet<Node> closedSet = new HashSet<Node>();

            startNode.GCost = 0;
            openQueue.Enqueue(startNode, (startNode.FCost, startNode.HCost));

            while (openQueue.Count > 0)
            {
                Node currentNode = openQueue.Dequeue();

                // Peek 最小优先级是 O(1)，Enqueue/Dequeue 都是 O(log n)
                if (closedSet.Contains(currentNode))
                {
                    continue;
                }

                if (currentNode == targetNode)
                {
                    return currentNode;
                }

                closedSet.Add(currentNode);
                EvaluateNeighbours(currentNode, targetNode, gridNodes, grid, openQueue, closedSet);
            }

            return null;
        }

        private static void EvaluateNeighbours(Node currentNode, Node targetNode, GridNodes gridNodes, IPathfindingGrid grid, PriorityQueue<Node, (int FCost, int HCost)> openQueue, HashSet<Node> closedSet)
        {
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                for (int yOffset = -1; yOffset <= 1; yOffset++)
                {
                    if (xOffset == 0 && yOffset == 0)
                    {
                        continue;
                    }

                    int neighbourX = currentNode.GridPosition.x + xOffset;
                    int neighbourY = currentNode.GridPosition.y + yOffset;

                    Node neighbour = GetWalkableNeighbour(neighbourX, neighbourY, gridNodes, grid, closedSet);

                    if (neighbour == null)
                    {
                        continue;
                    }

                    int movementPenalty = grid.GetMovementPenalty(neighbourX, neighbourY);
                    int newCostToNeighbour = currentNode.GCost + GetDistance(currentNode, neighbour) + movementPenalty;

                    // GCost 初始为 int.MaxValue（见 Node），所以"尚未发现"和"发现了更短路径"
                    // 是同一个比较分支，不需要再单独维护一个"是否已在开放列表里"的标记位。
                    if (newCostToNeighbour < neighbour.GCost)
                    {
                        neighbour.GCost = newCostToNeighbour;
                        neighbour.HCost = GetDistance(neighbour, targetNode);
                        neighbour.ParentNode = currentNode;

                        openQueue.Enqueue(neighbour, (neighbour.FCost, neighbour.HCost));
                    }
                }
            }
        }

        private static Node GetWalkableNeighbour(int x, int y, GridNodes gridNodes, IPathfindingGrid grid, HashSet<Node> closedSet)
        {
            if (x < 0 || x >= gridNodes.Width || y < 0 || y >= gridNodes.Height)
            {
                return null;
            }

            Node neighbour = gridNodes.GetNode(x, y);

            bool isObstacle = grid.GetMovementPenalty(x, y) == 0 || grid.GetObstaclePenalty(x, y) == 0;

            if (isObstacle || closedSet.Contains(neighbour))
            {
                return null;
            }

            return neighbour;
        }

        // 10/14 是把 sqrt(2) 近似成整数比例（14 ≈ 10*sqrt(2)），这样距离比较不需要引入浮点数。
        private static int GetDistance(Node from, Node to)
        {
            int distanceX = Mathf.Abs(from.GridPosition.x - to.GridPosition.x);
            int distanceY = Mathf.Abs(from.GridPosition.y - to.GridPosition.y);

            return distanceX > distanceY
                ? 14 * distanceY + 10 * (distanceX - distanceY)
                : 14 * distanceX + 10 * (distanceY - distanceX);
        }

        private static Stack<Vector3> CreatePathStack(Node endNode, IPathfindingGrid grid)
        {
            Stack<Vector3> path = new Stack<Vector3>();
            Node current = endNode;

            while (current != null)
            {
                path.Push(grid.GridToWorld(current.GridPosition));
                current = current.ParentNode;
            }

            return path;
        }
    }
}
