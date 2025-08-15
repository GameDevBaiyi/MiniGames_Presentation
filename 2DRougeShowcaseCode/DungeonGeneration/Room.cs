using System.Collections.Generic;
using UnityEngine;

namespace DungeonGunner.DungeonGeneration
{
    // 一次生成过程中的房间实例：模板决定了外形，Lower/UpperBounds 决定了它被放到世界坐标的哪里。
    // 只保留生成算法需要的字段——真实项目里 Room 还带敌人配置、音乐、探索状态等游戏内容，
    // 那些和"怎么把房间摆出来"无关，这里不展示。
    public sealed class Room
    {
        public string Id { get; }
        public GameObject Prefab { get; set; }

        // 模板本身的局部坐标边界（以左下角为原点）。
        public Vector2Int TemplateLowerBounds { get; set; }
        public Vector2Int TemplateUpperBounds { get; set; }

        // 放置到地牢里之后的世界坐标边界，用于重叠检测。
        public Vector2Int LowerBounds { get; set; }
        public Vector2Int UpperBounds { get; set; }

        public List<Doorway> Doorways { get; } = new List<Doorway>();
        public List<string> ChildRoomIds { get; } = new List<string>();
        public string ParentRoomId { get; set; }

        public bool IsPositioned { get; set; }

        public Room(string id)
        {
            Id = id;
        }

        public IEnumerable<Doorway> UnconnectedAvailableDoorways()
        {
            foreach (Doorway doorway in Doorways)
            {
                if (!doorway.IsConnected && !doorway.IsUnavailable)
                {
                    yield return doorway;
                }
            }
        }
    }
}
