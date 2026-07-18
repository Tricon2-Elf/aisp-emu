namespace AISpace.Network.Data;

public class MyRoomData(uint ownerId, uint ownerCharacterId, MyRoomStage roomStage, string roomName = "My Room", uint security = 0)
{
    public uint OwnerId = ownerId;
    public uint OwnerCharacterId = ownerCharacterId;
    public string RoomName = roomName;
    public MyRoomStage RoomStage = roomStage;

    public uint Security = security;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(OwnerId);
        writer.Write(OwnerCharacterId);
        writer.Write((uint)0);
        writer.WriteFixedJisString(RoomName, 46);
        writer.Write((byte)RoomStage);
        writer.Write((uint)0);
        writer.Write((uint)0);
        writer.Write(Security);
        writer.Write((uint)0);
        return writer.ToBytes();
    }
}
