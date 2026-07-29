using AISpace.Common.DAL;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Msg;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.Handlers.Msg;

public class CircleGetDataHandler(MainContext db) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.CircleGetDataRequest;
    public PacketType ResponseType => PacketType.CircleGetDataResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var list = new List<CircleData>();

        // Reload the character
        var cha = await db
            .Characters.Include(c => c.Circle)
            .FirstOrDefaultAsync(c => c.Id == session.CharacterId, ct);

        if (cha != null && cha.Circle != null)
        {
            // Use the current character ID as the main identifier
            uint myId = (uint)cha.Id;
            uint myCircleId = (uint)cha.Circle.Id;

            // 1. Add the circle to the list
            list.Add(new CircleData(myCircleId, cha.Circle.Name, myId));

            // 2. Send the composition (yourself as the leader)
            var membersList = new List<CircleMemberData>
            {
                new CircleMemberData
                {
                    AvatarId = myId,
                    Name = cha.Name,
                    Role = 2u, // Leader
                },
            };

            var notify = new CircleNotifyMember(myCircleId, membersList);
            await session.SendAsync(PacketType.CircleNotifyMember, notify.ToBytes(), ct);
        }

        // 3. Send the final response
        var response = new CircleGetDataResponse(0, list);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
