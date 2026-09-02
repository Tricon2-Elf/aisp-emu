using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class MailPostHandler(
    ICharacterRepository characters,
    SharedState state,
    IWordFilter wordFilter
) : PacketHandlerBase<MailPostRequest, MailPostResponse>, IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.MailPostRequest;
    public override PacketType ResponseType => PacketType.MailPostResponse;
    public override ServerType ServerType => ServerType.Msg;

    public override async Task<MailPostResponse?> HandleAsync(
        MailPostRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (session.CharacterId == 0)
            return new MailPostResponse(1, new MailData());

        if (wordFilter.ContainsBlockedWord(WordFilterLevel.Complete, request.Subject, request.Body))
            return new MailPostResponse(1, new MailData());

        var recipient = await ResolveRecipientAsync(request, ct);
        if (recipient is null)
            return new MailPostResponse(1, new MailData());

        var senderName = session.Character?.Name;
        if (string.IsNullOrEmpty(senderName))
        {
            var sender = await characters.GetByIdAsync(checked((int)session.CharacterId), ct);
            senderName = sender?.Name ?? string.Empty;
        }

        var date = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss");
        if (date.Length > MailData.DateLength)
            date = date[..MailData.DateLength];

        var mail = new MailData
        {
            MailId = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Type = 0,
            Flags = 0,
            SenderId = session.CharacterId,
            SenderName = senderName,
            DistId = (uint)recipient.Id,
            DistName = recipient.Name,
            Date = date,
            Subject = request.Subject,
            Body = request.Body,
        };

        var notify = new NotifyNewMail(mail).ToBytes();
        foreach (var client in state.GetOnlineMsgClientsByCharacterIds(new[] { recipient.Id }))
        {
            if (client.ConnectionId == session.ConnectionId)
                continue;
            _ = client.SendAsync(PacketType.NotifyNewMail, notify, ct);
        }

        // Post-mail response is stored client-side in the sent box; type 1 matches that folder.
        mail.Type = 1;
        return new MailPostResponse(0, mail);
    }

    private async Task<DAL.Entities.Character?> ResolveRecipientAsync(
        MailPostRequest request,
        CancellationToken ct
    )
    {
        if (request.DistId != 0)
            return await characters.GetByIdAsync(checked((int)request.DistId), ct);

        if (!string.IsNullOrEmpty(request.DistName))
            return await characters.GetByNameAsync(request.DistName, ct);

        return null;
    }
}
