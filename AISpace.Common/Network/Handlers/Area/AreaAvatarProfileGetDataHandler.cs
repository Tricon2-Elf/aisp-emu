using AISpace.Common.Game;

namespace AISpace.Common.Network.Handlers;

public class AreaAvatarProfileGetDataHandler(SharedState state) : IPacketHandler
{
    public PacketType RequestType => PacketType.AvatarProfileGetDataRequest;
    public PacketType ResponseType => (PacketType)0xB670; 
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var reader = new PacketReader(payload.Span);
        uint targetObjId = reader.ReadUInt();

        var target = state.AreaClients.Values.FirstOrDefault(c => c.CharacterId == targetObjId);
        var cha = target?.User?.Characters.FirstOrDefault();

        var writer = new PacketWriter();
        writer.Write((uint)0); // Result
        writer.Write(targetObjId);

        if (cha != null) {
            writer.WriteFixedJisString(cha.Like1 ?? "", 31);
            writer.WriteFixedJisString(cha.Like2 ?? "", 31);
            writer.WriteFixedJisString(cha.Like3 ?? "", 31);
            writer.WriteFixedJisString(cha.LikeDesc1 ?? "", 91);
            writer.WriteFixedJisString(cha.LikeDesc2 ?? "", 91);
            writer.WriteFixedJisString(cha.LikeDesc3 ?? "", 91);
            writer.WriteFixedJisString(cha.AvatarDesc ?? "", 901);
            
            // ДОБИВАЕМ ПАДДИНГОМ: 1280 - (4+4+31*3+91*3+901) = 5 байт
            writer.Write(new byte[5]); 
        } else {
            // Если перс не найден — шлем 1272 байта нулей после заголовка (8 байт)
            writer.Write(new byte[1272]);
        }

        await connection.SendAsync(ResponseType, writer.ToBytes(), ct);
    }
}