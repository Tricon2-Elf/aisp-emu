using AISpace.Network;
using AISpace.Common.Network.Packets.Msg;

namespace AISpace.Common.Handlers.Msg;

public class CircleChatPostHandler(ILogger<CircleChatPostHandler> logger, SharedState state) : IPacketHandler
{
    public PacketType RequestType => PacketType.CircleChatPostRequest;
    public PacketType ResponseType => PacketType.CircleChatPostResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        // Read the request (CircleID, Message, BalloonID)
        var reader = new PacketReader(payload.Span);
        uint circleId = reader.ReadUInt();
        string message = reader.ReadString("Shift_JIS");
        uint balloonId = reader.ReadUInt();

        logger.LogInformation($"[CIRCLE CHAT] From:{connection.CharacterId} Circle:{circleId}: {message}");

        // 1. Response to the sender
        var response = new CmdExecResponse(0, 0);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);

        // 2. Preparation for distribution (recv_circle_chat_forward)
        var writer = new PacketWriter();
        writer.Write(circleId); // ID of the circle
        writer.Write(connection.CharacterId); // Who sent
        writer.Write(message, "Shift_JIS"); // Text + \0
        writer.Write(balloonId); // Type of balloon

        byte[] forwardData = writer.ToBytes();

        // 3. Distribution to the circle members (except yourself)
        foreach (var client in state.MsgClients.Values)
        {
            if (client.IsAuthenticated && client.Id != connection.Id)
            {
                // Here in the future we need to check: if (client.InCircle == circleId)
                await client.SendAsync(PacketType.CircleChatForwardNotify, forwardData, ct);
            }
        }
    }
}
