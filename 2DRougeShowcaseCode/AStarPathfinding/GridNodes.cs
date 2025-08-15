using System;
using UnityEngine;

namespace DungeonGunner.AStarPathfinding
{
    // 房间局部坐标系下的寻路网格：每个格子对应一个 Node，宽高在构造时一次性分配好，
    // 避免寻路过程中反复创建节点对象。
    public sealed class GridNodes
    {
        private readonly Node[,] _nodes;

        public int Width { get; }
        public int Height { get; }

        public GridNodes(int width, int height)
        {
            Width = width;
            Height = height;
            _nodes = new Node[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _nodes[x, y] = new Node(new Vector2Int(x, y));
                }
            }
        }

        public Node GetNode(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                throw new ArgumentOutOfRangeException($"({x},{y}) is outside the {Width}x{Height} grid.");
            }

            return _nodes[x, y];
        }
    }
}
