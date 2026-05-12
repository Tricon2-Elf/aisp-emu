using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Msg;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Msg;

public class CircleChatPostHandler(ILogger<CircleChatPostHandler> logger, SharedState state) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.CircleChatPostRequest;
    public PacketType ResponseType => PacketType.CircleChatPostResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        // Read the request (CircleID, Message, BalloonID)
        var reader = new PacketReader(payload.Span);
        uint circleId = reader.ReadUInt();
        string message = reader.ReadString("Shift_JIS");
        uint balloonId = reader.ReadUInt();

        logger.LogInformation($"[CIRCLE CHAT] From:{session.CharacterId} Circle:{circleId}: {message}");

        // 1. Response to the sender
        var response = new CmdExecResponse(0, 0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);

        // 2. Preparation for distribution (recv_circle_chat_forward)
        var writer = new PacketWriter();
        writer.Write(circleId); // ID of the circle
        writer.Write(session.CharacterId); // Who sent
        writer.Write(message, "Shift_JIS"); // Text + \0
        writer.Write(balloonId); // Type of balloon

        byte[] forwardData = writer.ToBytes();

        // 3. Distribution to the circle members (except yourself)
        foreach (var client in state.GetServerClients(ServerType.Msg))
        {
            if (client.IsAuthenticated && client.ConnectionId != session.ConnectionId)
            {
                // Here in the future we need to check: if (client.InCircle == circleId)
                await client.SendAsync(PacketType.CircleChatForwardNotify, forwardData, ct);
            }
        }
    }
}
