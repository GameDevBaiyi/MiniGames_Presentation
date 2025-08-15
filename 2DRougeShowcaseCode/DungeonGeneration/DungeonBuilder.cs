using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DungeonGunner.DungeonGeneration
{
    // 地牢生成的核心算法。随机性体现在三层：随机选节点图（拓扑）、随机选房间模板（外形）、
    // 随机选父房间的连接门；重叠检测是唯一的硬约束，失败就换一个门/模板重试，
    // 同一张节点图反复重试到上限还是不行就换一张图，直到用完全部尝试次数。
    public sealed class DungeonBuilder : MonoBehaviour
    {
        private const int MaxGraphSelectionAttempts = 10;
        private const int MaxPlacementAttemptsPerGraph = 1000;

        [Tooltip("生成出的房间 GameObject 挂在这个节点下面")]
        [SerializeField] private Transform _roomsParent;

        // 通知外部系统"入口房间已经生成好了"，替代原本直接调用 GameManager 单例的写法，
        // 让 DungeonBuilder 不需要认识游戏管理器就能完成职责。
        public event Action<Room> EntranceRoomCreated;

        private readonly Dictionary<string, Room> _roomsById = new Dictionary<string, Room>();

        public IReadOnlyDictionary<string, Room> Rooms => _roomsById;

        // 生成一层随机地牢；roomTemplates 是这一层可用的房间外形池，candidateGraphs 是可选的拓扑结构。
        // 返回 false 代表用完所有尝试次数仍未能生成无重叠的布局。
        public bool GenerateDungeon(IReadOnlyList<RoomTemplate> roomTemplates, IReadOnlyList<RoomNodeGraphSO> candidateGraphs)
        {
            if (roomTemplates == null || roomTemplates.Count == 0)
            {
                throw new ArgumentException("At least one room template is required.", nameof(roomTemplates));
            }

            if (candidateGraphs == null || candidateGraphs.Count == 0)
            {
                throw new ArgumentException("At least one room node graph is required.", nameof(candidateGraphs));
            }

            for (int graphAttempt = 0; graphAttempt < MaxGraphSelectionAttempts; graphAttempt++)
            {
                RoomNodeGraphSO graph = candidateGraphs[Random.Range(0, candidateGraphs.Count)];

                for (int placementAttempt = 0; placementAttempt < MaxPlacementAttemptsPerGraph; placementAttempt++)
                {
                    ClearDungeon();

                    if (TryPlaceAllRooms(graph, roomTemplates))
                    {
                        InstantiateRoomGameObjects();
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryPlaceAllRooms(RoomNodeGraphSO graph, IReadOnlyList<RoomTemplate> roomTemplates)
        {
            RoomNode entranceNode = graph.FindByType(RoomNodeType.Entrance);

            if (entranceNode == null)
            {
                return false;
            }

            Queue<RoomNode> openNodes = new Queue<RoomNode>();
            openNodes.Enqueue(entranceNode);

            while (openNodes.Count > 0)
            {
                RoomNode node = openNodes.Dequeue();

                foreach (RoomNode child in graph.GetChildren(node))
                {
                    openNodes.Enqueue(child);
                }

                if (node.Type == RoomNodeType.Entrance)
                {
                    RoomTemplate template = GetRandomRoomTemplate(roomTemplates, RoomNodeType.Entrance);
                    Room entranceRoom = CreateRoomFromTemplate(template, node);
                    entranceRoom.IsPositioned = true;

                    _roomsById.Add(entranceRoom.Id, entranceRoom);
                    EntranceRoomCreated?.Invoke(entranceRoom);
                }
                else
                {
                    Room parentRoom = _roomsById[node.ParentId];

                    if (!TryPlaceRoomAgainstParent(node, parentRoom, roomTemplates))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // 依次尝试父房间的每一个未使用门，直到成功放下一个不重叠的房间，或者门都试完了还是不行。
        private bool TryPlaceRoomAgainstParent(RoomNode node, Room parentRoom, IReadOnlyList<RoomTemplate> roomTemplates)
        {
            while (true)
            {
                List<Doorway> availableDoorways = parentRoom.UnconnectedAvailableDoorways().ToList();

                if (availableDoorways.Count == 0)
                {
                    return false;
                }

                Doorway parentDoorway = availableDoorways[Random.Range(0, availableDoorways.Count)];
                RoomTemplate template = GetTemplateConsistentWithParentDoorway(node, parentDoorway, roomTemplates);
                Room room = CreateRoomFromTemplate(template, node);

                if (TryPlaceRoom(parentRoom, parentDoorway, room))
                {
                    room.IsPositioned = true;
                    _roomsById.Add(room.Id, room);
                    return true;
                }
            }
        }

        private RoomTemplate GetTemplateConsistentWithParentDoorway(RoomNode node, Doorway parentDoorway, IReadOnlyList<RoomTemplate> roomTemplates)
        {
            // 走廊模板是按朝向单独制作的，父门朝南北就必须选南北向走廊，否则拼不上。
            if (node.Type == RoomNodeType.CorridorNorthSouth || node.Type == RoomNodeType.CorridorEastWest)
            {
                bool parentIsNorthSouth = parentDoorway.Orientation == DoorwayOrientation.North || parentDoorway.Orientation == DoorwayOrientation.South;
                RoomNodeType corridorType = parentIsNorthSouth ? RoomNodeType.CorridorNorthSouth : RoomNodeType.CorridorEastWest;
                return GetRandomRoomTemplate(roomTemplates, corridorType);
            }

            return GetRandomRoomTemplate(roomTemplates, node.Type);
        }

        // 把 room 对齐到 parentDoorway 的位置，成功且不重叠则把两侧门都标记为已连接。
        private bool TryPlaceRoom(Room parentRoom, Doorway parentDoorway, Room room)
        {
            Doorway matchingDoorway = FindOppositeDoorway(parentDoorway, room.Doorways);

            if (matchingDoorway == null)
            {
                parentDoorway.IsUnavailable = true;
                return false;
            }

            Vector2Int parentDoorwayWorldPosition = parentRoom.LowerBounds + parentDoorway.Position - parentRoom.TemplateLowerBounds;
            Vector2Int adjustment = AdjustmentForOrientation(matchingDoorway.Orientation);

            room.LowerBounds = parentDoorwayWorldPosition + adjustment + room.TemplateLowerBounds - matchingDoorway.Position;
            room.UpperBounds = room.LowerBounds + room.TemplateUpperBounds - room.TemplateLowerBounds;

            if (FindOverlappingRoom(room) != null)
            {
                parentDoorway.IsUnavailable = true;
                return false;
            }

            parentDoorway.IsConnected = true;
            parentDoorway.IsUnavailable = true;
            matchingDoorway.IsConnected = true;
            matchingDoorway.IsUnavailable = true;
            return true;
        }

        private static Vector2Int AdjustmentForOrientation(DoorwayOrientation orientation)
        {
            switch (orientation)
            {
                case DoorwayOrientation.North: return new Vector2Int(0, -1);
                case DoorwayOrientation.East: return new Vector2Int(-1, 0);
                case DoorwayOrientation.South: return new Vector2Int(0, 1);
                case DoorwayOrientation.West: return new Vector2Int(1, 0);
                default: return Vector2Int.zero;
            }
        }

        private static Doorway FindOppositeDoorway(Doorway parentDoorway, IReadOnlyList<Doorway> candidateDoorways)
        {
            foreach (Doorway doorway in candidateDoorways)
            {
                bool isOpposite =
                    (parentDoorway.Orientation == DoorwayOrientation.East && doorway.Orientation == DoorwayOrientation.West) ||
                    (parentDoorway.Orientation == DoorwayOrientation.West && doorway.Orientation == DoorwayOrientation.East) ||
                    (parentDoorway.Orientation == DoorwayOrientation.North && doorway.Orientation == DoorwayOrientation.South) ||
                    (parentDoorway.Orientation == DoorwayOrientation.South && doorway.Orientation == DoorwayOrientation.North);

                if (isOpposite)
                {
                    return doorway;
                }
            }

            return null;
        }

        private Room FindOverlappingRoom(Room roomToTest)
        {
            foreach (Room room in _roomsById.Values)
            {
                if (room.Id == roomToTest.Id || !room.IsPositioned)
                {
                    continue;
                }

                if (BoundsOverlap(roomToTest, room))
                {
                    return room;
                }
            }

            return null;
        }

        private static bool BoundsOverlap(Room a, Room b)
        {
            bool overlapsX = IntervalsOverlap(a.LowerBounds.x, a.UpperBounds.x, b.LowerBounds.x, b.UpperBounds.x);
            bool overlapsY = IntervalsOverlap(a.LowerBounds.y, a.UpperBounds.y, b.LowerBounds.y, b.UpperBounds.y);
            return overlapsX && overlapsY;
        }

        private static bool IntervalsOverlap(int minA, int maxA, int minB, int maxB)
        {
            return Mathf.Max(minA, minB) <= Mathf.Min(maxA, maxB);
        }

        private static RoomTemplate GetRandomRoomTemplate(IReadOnlyList<RoomTemplate> roomTemplates, RoomNodeType type)
        {
            List<RoomTemplate> matching = new List<RoomTemplate>();

            foreach (RoomTemplate template in roomTemplates)
            {
                if (template.RoomNodeType == type)
                {
                    matching.Add(template);
                }
            }

            return matching.Count == 0 ? null : matching[Random.Range(0, matching.Count)];
        }

        private static Room CreateRoomFromTemplate(RoomTemplate template, RoomNode node)
        {
            Room room = new Room(node.Id)
            {
                Prefab = template.Prefab,
                TemplateLowerBounds = template.LowerBounds,
                TemplateUpperBounds = template.UpperBounds,
                LowerBounds = template.LowerBounds,
                UpperBounds = template.UpperBounds,
                ParentRoomId = node.ParentId
            };

            room.ChildRoomIds.AddRange(node.ChildIds);
            room.Doorways.AddRange(CloneDoorways(template.Doorways));

            return room;
        }

        // 深拷贝门列表：同一个模板可能被用来生成多个房间实例，各自的连接状态不能共享。
        private static IEnumerable<Doorway> CloneDoorways(IReadOnlyList<Doorway> source)
        {
            foreach (Doorway doorway in source)
            {
                yield return new Doorway(doorway);
            }
        }

        private void InstantiateRoomGameObjects()
        {
            foreach (Room room in _roomsById.Values)
            {
                Vector3 roomPosition = new Vector3(
                    room.LowerBounds.x - room.TemplateLowerBounds.x,
                    room.LowerBounds.y - room.TemplateLowerBounds.y,
                    0f);

                Instantiate(room.Prefab, roomPosition, Quaternion.identity, _roomsParent);
            }
        }

        private void ClearDungeon()
        {
            if (_roomsParent != null)
            {
                for (int i = _roomsParent.childCount - 1; i >= 0; i--)
                {
                    Destroy(_roomsParent.GetChild(i).gameObject);
                }
            }

            _roomsById.Clear();
        }
    }
}
