using System.Collections.Generic;

namespace DungeonGunner.DungeonGeneration
{
    // 房间节点图里的一个节点：只描述"这里应该有一个什么类型的房间"和父子关系，
    // 不涉及具体选中了哪个房间模板——模板是生成时才随机决定的。
    [System.Serializable]
    public sealed class RoomNode
    {
        public string Id;
        public RoomNodeType Type;
        public string ParentId;
        public List<string> ChildIds = new List<string>();
    }
}
