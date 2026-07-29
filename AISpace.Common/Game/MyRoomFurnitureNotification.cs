using AISpace.Network;

namespace AISpace.Common.Game;

internal static class MyRoomFurnitureNotification
{
    public static async Task BroadcastToRoomAsync(
        SharedState state,
        IPlayerSession source,
        uint roomId,
        PacketType packetType,
        byte[] payload,
        bool includeSource,
        CancellationToken ct
    )
    {
        var recipients = state
            .GetAreaPeers(source, includeSource)
            .Where(peer => peer.MyRoomId == roomId);
        foreach (var peer in recipients)
            await peer.SendAsync(packetType, payload, ct);
    }
}
