namespace aisp.Network.Data;

public class MyRoomData(
    uint roomId,
    uint ownerCharacterId,
    MyRoomStage roomStage,
    string roomName = "My Room",
    MyRoomSecurity security = MyRoomSecurity.Private
)
{
    public uint RoomId = roomId;
    public uint OwnerCharacterId = ownerCharacterId;
    public string RoomName = roomName;
    public MyRoomStage RoomStage = roomStage;

    public MyRoomSecurity Security = security;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(RoomId);
        writer.Write(OwnerCharacterId);
        writer.Write((uint)0);
        writer.WriteFixedJisString(RoomName, 46);
        writer.Write((byte)RoomStage);
        writer.Write((uint)0);
        writer.Write((uint)0);
        writer.Write((uint)Security);
        writer.Write((uint)0);
        return writer.ToBytes();
    }
}
