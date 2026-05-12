using AISpace.Common.Game;
using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class AreaOtherProfileTextHandler(SharedState state) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.OtherProfileTextRequest;
    public PacketType ResponseType => (PacketType)0xDDEE; // GetMyAvatarMyprofileDataResponse (common format)
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var reader = new PacketReader(payload.Span);
        uint targetObjId = reader.ReadUInt();

        // Find the character whose profile we are viewing
        var target = state.GetAreaSessionByCharacterId(targetObjId, session.MapId, session.ChannelId);
        var cha = target?.Character ?? target?.User?.Characters.FirstOrDefault();

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
            // If the player has already logged out, send empty strings of the same size
            writer.Write(new byte[31 * 3 + 91 * 3 + 901]);
        }

        await session.SendAsync(ResponseType, writer.ToBytes(), ct);
    }
}
