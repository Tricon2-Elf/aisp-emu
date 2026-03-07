using AISpace.Common.Network.Packets.Msg;
using AISpace.Network;
using AISpace.Network.Packets.Msg;

namespace AISpace.Common.Handlers.Msg;

public class CircleGetDataHandler(MainContext db) : IPacketHandler
{
    public PacketType RequestType => PacketType.CircleGetDataRequest;
    public PacketType ResponseType => PacketType.CircleGetDataResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var list = new List<CircleData>();

        if (connection.User != null)
        {
            // Reload the character
            var cha = await db.Characters.Include(c => c.Circle).FirstOrDefaultAsync(c => c.Id == connection.CharacterId, ct);

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
                await connection.SendAsync(PacketType.CircleNotifyMember, notify.ToBytes(), ct);
            }
        }

        // 3. Send the final response
        var response = new CircleGetDataResponse(0, list);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
