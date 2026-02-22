using AISpace.Common.Game;

namespace AISpace.Common.Network.Handlers;

public class AreaOtherProfileTextHandler(SharedState state) : IPacketHandler
{
    public PacketType RequestType => PacketType.OtherProfileTextRequest;
    public PacketType ResponseType => (PacketType)0xDDEE; // GetMyAvatarMyprofileDataResponse (общий формат)
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var reader = new PacketReader(payload.Span);
        uint targetObjId = reader.ReadUInt();

        // Ищем того, чей профиль мы смотрим
        var target = state.AreaClients.Values.FirstOrDefault(c => c.CharacterId == targetObjId);
        var cha = target?.User?.Characters.FirstOrDefault();

        var writer = new PacketWriter();
        writer.Write((uint)0); // Result
        writer.Write((uint)0); // _0x0000
        writer.Write((uint)0); // _0x0004
        writer.Write((uint)0); // _0x0008

        if (cha != null)
        {
            writer.WriteFixedJisString(cha.Like1 ?? "None", 31);
            writer.WriteFixedJisString(cha.Like2 ?? "None", 31);
            writer.WriteFixedJisString(cha.Like3 ?? "None", 31);
            writer.WriteFixedJisString(cha.LikeDesc1 ?? "", 91);
            writer.WriteFixedJisString(cha.LikeDesc2 ?? "", 91);
            writer.WriteFixedJisString(cha.LikeDesc3 ?? "", 91);
            writer.WriteFixedJisString(cha.AvatarDesc ?? "Hello!", 901);
        }
        else
        {
            // Если игрок уже вышел, шлем пустые строки того же размера
            writer.Write(new byte[31 * 3 + 91 * 3 + 901]);
        }

        await connection.SendAsync(ResponseType, writer.ToBytes(), ct);
    }
}