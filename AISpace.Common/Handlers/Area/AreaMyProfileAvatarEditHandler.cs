using AISpace.Common.DAL;
using AISpace.Common.Game;
using AISpace.Network;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.Handlers.Area;

public class AreaMyProfileAvatarEditHandler(MainContext db) : IPacketHandler
{
    public PacketType RequestType => PacketType.MyProfileAvatarEditRequest;
    public PacketType ResponseType => PacketType.MyProfileAvatarEditResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var reader = new PacketReader(payload.Span);
        reader.ReadBytes(12);

        var l1 = reader.ReadFixedString(31, "Shift_JIS");
        var l2 = reader.ReadFixedString(31, "Shift_JIS");
        var l3 = reader.ReadFixedString(31, "Shift_JIS");

        var d1 = reader.ReadFixedString(91, "Shift_JIS");
        var d2 = reader.ReadFixedString(91, "Shift_JIS");
        var d3 = reader.ReadFixedString(91, "Shift_JIS");

        var desc = reader.ReadFixedString(901, "Shift_JIS");

        var cha = await db.Characters.FirstOrDefaultAsync(c => c.Id == session.CharacterId, ct);
        if (cha != null)
        {
            cha.Like1 = l1;
            cha.Like2 = l2;
            cha.Like3 = l3;
            cha.LikeDesc1 = d1;
            cha.LikeDesc2 = d2;
            cha.LikeDesc3 = d3;
            cha.AvatarDesc = desc;
            await db.SaveChangesAsync(ct);
        }

        var writer = new PacketWriter();
        writer.Write((uint)0);
        await session.SendAsync(ResponseType, writer.ToBytes(), ct);
    }
}
