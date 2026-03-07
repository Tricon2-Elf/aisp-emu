using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Msg;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Msg;

public class CircleCreateHandler(MainContext db, ILogger<CircleCreateHandler> logger) : PacketHandlerBase<CircleCreateRequest, CircleCreateResponse>
{
    public override PacketType RequestType => PacketType.CircleCreateRequest;
    public override PacketType ResponseType => PacketType.CircleCreateResponse;
    public override MessageDomain Domain => MessageDomain.Msg;

    public override async Task<CircleCreateResponse?> HandleAsync(CircleCreateRequest request, IPlayerSession session, CancellationToken ct = default)
    {
        if (session.User == null)
            return new CircleCreateResponse(1, null);
        var character = await db.Characters.FirstOrDefaultAsync(c => c.Id == session.CharacterId, ct);
        if (character == null)
            return new CircleCreateResponse(1, null);
        if (character.CircleId != null)
            return new CircleCreateResponse(2, null);

        var circle = new Circle
        {
            Name = request.Name,
            LeaderCharacterId = character.Id,
            CreatedAt = DateTime.UtcNow,
        };

        //db.Circles.Add(circle);
        //await db.SaveChangesAsync(ct);

        //character.CircleId = circle.Id;
        //await db.SaveChangesAsync(ct);

        var membersList = new List<CircleMemberData>();
        var notifyPacket = new CircleNotifyMember((uint)circle.Id, membersList);
        await session.SendAsync(PacketType.CircleNotifyMember, notifyPacket.ToBytes(), ct);

        var cData = new CircleData((uint)circle.Id, circle.Name, (uint)character.Id);
        return new CircleCreateResponse(0, cData);
    }
}
