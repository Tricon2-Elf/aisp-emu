using System.Globalization;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class MailBoxGetDataHandler(IMailRepository mailRepository)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public const int MaxMailCount = MailRepository.MaxPageSize;

    public PacketType RequestType => PacketType.MailBoxGetDataRequest;

    public PacketType ResponseType => PacketType.MailBoxGetDataResponse;

    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (session.CharacterId == 0)
        {
            await session.SendAsync(ResponseType, new MailBoxGetDataResponse(1, []).ToBytes(), ct);
            return;
        }

        var messages = await mailRepository.ListRecentAsync(
            checked((int)session.CharacterId),
            MaxMailCount,
            ct
        );
        var wireMessages = messages.Select(ToMailData).ToList();
        await session.SendAsync(
            ResponseType,
            new MailBoxGetDataResponse(0, wireMessages).ToBytes(),
            ct
        );
    }

    private static MailData ToMailData(MailboxEntry entry) =>
        new()
        {
            MailId = checked((ulong)entry.Message.Id),
            Type = entry.Type,
            Flags = entry.IsRead ? 1u : 0u,
            SenderId = checked((uint)entry.Message.SenderCharacterId),
            SenderName = entry.Message.SenderName,
            DistId = checked((uint)entry.Message.DestinationId),
            DistName = entry.Message.DestinationName,
            Date = entry.Message.CreatedAtUtc.ToString(
                "yyyy/MM/dd HH:mm:ss",
                CultureInfo.InvariantCulture
            ),
            Subject = entry.Message.Subject,
            Body = entry.Message.Body,
        };
}
