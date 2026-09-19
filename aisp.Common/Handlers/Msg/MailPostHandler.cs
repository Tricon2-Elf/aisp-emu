using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class MailPostHandler(
    ICharacterRepository characters,
    IMailRepository mailRepository,
    SharedState state,
    IWordFilter wordFilter,
    ICircleRepository? circles = null
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

        var destination = await ResolveDestinationAsync(
            request,
            checked((int)session.CharacterId),
            ct
        );
        if (destination is null)
            return new MailPostResponse(1, new MailData());

        var senderName = session.Character?.Name;
        if (string.IsNullOrEmpty(senderName))
        {
            var sender = await characters.GetByIdAsync(checked((int)session.CharacterId), ct);
            senderName = sender?.Name ?? string.Empty;
        }

        var createdAtUtc = DateTime.UtcNow;
        var storedMail = await mailRepository.AddAsync(
            new MailMessage
            {
                SenderCharacterId = checked((int)session.CharacterId),
                SenderName = senderName,
                DestinationType = destination.Type,
                DestinationId = destination.Id,
                DestinationName = destination.Name,
                Subject = request.Subject,
                Body = request.Body,
                CreatedAtUtc = createdAtUtc,
                Recipients =
                [
                    .. destination.Recipients.Select(x => new MailRecipient
                    {
                        CharacterId = x.Id,
                        InboxType =
                            destination.Type == MailDestinationType.Circle ? (byte)2 : (byte)0,
                    }),
                ],
            },
            ct
        );

        var date = createdAtUtc.ToString("yyyy/MM/dd HH:mm:ss");
        if (date.Length > MailData.DateLength)
            date = date[..MailData.DateLength];

        var mail = new MailData
        {
            MailId = checked((ulong)storedMail.Id),
            Type = 0,
            Flags = 0,
            SenderId = session.CharacterId,
            SenderName = senderName,
            DistId = checked((uint)destination.Id),
            DistName = destination.Name,
            Date = date,
            Subject = request.Subject,
            Body = request.Body,
        };

        mail.Type = destination.Type == MailDestinationType.Circle ? 2u : 0u;
        var notify = new NotifyNewMail(mail).ToBytes();
        foreach (var recipient in destination.Recipients)
        {
            foreach (var client in state.GetOnlineMsgClientsByCharacterId(recipient.Id))
            {
                if (client.ConnectionId == session.ConnectionId)
                    continue;
                _ = client.SendAsync(PacketType.NotifyNewMail, notify, ct);
            }
        }

        // Post-mail response is stored client-side in the sent box; type 1 matches that folder.
        mail.Type = 1;
        return new MailPostResponse(0, mail);
    }

    private async Task<MailDestination?> ResolveDestinationAsync(
        MailPostRequest request,
        int senderCharacterId,
        CancellationToken ct
    )
    {
        DAL.Entities.Character? character = null;
        DAL.Entities.Circle? circle = null;
        if (request.DistId != 0)
        {
            var id = checked((int)request.DistId);
            character = await characters.GetByIdAsync(id, ct);
            if (circles is not null)
                circle = await circles.GetByIdAsync(id, ct);
        }
        else if (!string.IsNullOrEmpty(request.DistName))
        {
            character = await characters.GetByNameAsync(request.DistName, ct);
            if (character is null && circles is not null)
                circle = await circles.GetByNameAsync(request.DistName, ct);
        }

        if (
            character is not null
            && (string.IsNullOrEmpty(request.DistName) || request.DistName == character.Name)
        )
        {
            return new MailDestination(
                MailDestinationType.Character,
                character.Id,
                character.Name,
                [character]
            );
        }

        if (
            circle is null
            || (!string.IsNullOrEmpty(request.DistName) && request.DistName != circle.Name)
            || circles is null
        )
            return null;

        var members = await circles.GetMembersAsync(circle.Id, ct);
        if (members.All(x => x.CharacterId != senderCharacterId))
            return null;

        var recipients = members
            .Where(x => x.CharacterId != senderCharacterId)
            .Select(x => x.Character)
            .DistinctBy(x => x.Id)
            .ToList();
        if (recipients.Count == 0)
            return null;

        return new MailDestination(MailDestinationType.Circle, circle.Id, circle.Name, recipients);
    }

    private sealed record MailDestination(
        MailDestinationType Type,
        int Id,
        string Name,
        IReadOnlyList<DAL.Entities.Character> Recipients
    );
}
