namespace DungeonGunner.DungeonGeneration
{
    // 房间节点图里一个节点代表的房间类型。走廊按朝向拆成南北/东西两种，
    // 是因为走廊模板本身就是按朝向分开制作的，不能像普通房间那样随便挑一个。
    public enum RoomNodeType
    {
        Entrance,
        CorridorNorthSouth,
        CorridorEastWest,
        Standard,
        BossRoom
    }
}
