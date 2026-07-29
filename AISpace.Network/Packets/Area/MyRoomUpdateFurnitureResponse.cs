namespace AISpace.Network.Packets.Area;

/// <summary>recv_myroom_update_furniture_r (0x50A3): four-byte result code.</summary>
public sealed class MyRoomUpdateFurnitureResponse(uint result) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes() => MyRoomFurniturePacketEncoding.WriteResult(Result);
}
