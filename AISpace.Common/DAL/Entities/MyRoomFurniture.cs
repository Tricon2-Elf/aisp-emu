namespace AISpace.Common.DAL.Entities;

public sealed class MyRoomFurniture
{
    public int RoomId { get; set; }
    public Room Room { get; set; } = default!;
    public uint FurnitureId { get; set; }
    public int ItemId { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }
    public byte DirectionX { get; set; }
    public byte DirectionY { get; set; }
    public Nicotv? Nicotv { get; set; }
}
