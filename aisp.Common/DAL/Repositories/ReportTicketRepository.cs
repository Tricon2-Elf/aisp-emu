using aisp.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.DAL.Repositories;

public sealed record ReportTicketCreateRequest(
    int ReporterUserId,
    string ReporterUsername,
    int ReporterCharacterId,
    string ReporterCharacterName,
    string Reason,
    uint MapId,
    int ChannelId,
    string MapName,
    IReadOnlyList<ReportTicketPlayerSnapshot> Players,
    IReadOnlyList<ReportTicketChatSnapshot> ChatMessages
);

public sealed record ReportTicketPlayerSnapshot(
    int UserId,
    string Username,
    int CharacterId,
    string CharacterName
);

public sealed record ReportTicketChatSnapshot(
    DateTime CreatedAt,
    int CharacterId,
    string CharacterName,
    string Message,
    bool Rejected
);

public interface IReportTicketRepository
{
    Task<ReportTicket> CreateAsync(ReportTicketCreateRequest request, CancellationToken ct = default);

    Task<(IReadOnlyList<ReportTicket> Items, int Total)> ListAsync(
        ReportTicketStatus? status = null,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default
    );

    Task<ReportTicket?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<bool> ResolveAsync(
        long id,
        int resolvedByUserId,
        string resolutionAction,
        CancellationToken ct = default
    );
}

public sealed class ReportTicketRepository(MainContext db) : IReportTicketRepository
{
    public const int MaxPageSize = 100;

    public async Task<ReportTicket> CreateAsync(
        ReportTicketCreateRequest request,
        CancellationToken ct = default
    )
    {
        var ticket = new ReportTicket
        {
            ReporterUserId = request.ReporterUserId,
            ReporterUsername = request.ReporterUsername,
            ReporterCharacterId = request.ReporterCharacterId,
            ReporterCharacterName = request.ReporterCharacterName,
            Reason = request.Reason,
            MapId = request.MapId,
            ChannelId = request.ChannelId,
            MapName = request.MapName,
            Status = ReportTicketStatus.Open,
            CreatedAt = DateTime.UtcNow,
            Players = request
                .Players.Select(player => new ReportTicketPlayer
                {
                    UserId = player.UserId,
                    Username = player.Username,
                    CharacterId = player.CharacterId,
                    CharacterName = player.CharacterName,
                })
                .ToList(),
            ChatMessages = request
                .ChatMessages.Select(chat => new ReportTicketChatMessage
                {
                    CreatedAt = chat.CreatedAt,
                    CharacterId = chat.CharacterId,
                    CharacterName = chat.CharacterName,
                    Message = chat.Message,
                    Rejected = chat.Rejected,
                })
                .ToList(),
        };
        db.ReportTickets.Add(ticket);
        await db.SaveChangesAsync(ct);
        return ticket;
    }

    public async Task<(IReadOnlyList<ReportTicket> Items, int Total)> ListAsync(
        ReportTicketStatus? status = null,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default
    )
    {
        var query = db.ReportTickets.AsNoTracking();
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var total = await query.CountAsync(ct);
        var pageSize = Math.Clamp(take, 1, MaxPageSize);
        var offset = Math.Max(skip, 0);
        var items = await query
            .Include(x => x.Players)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip(offset)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public Task<ReportTicket?> GetByIdAsync(long id, CancellationToken ct = default) =>
        db
            .ReportTickets.AsNoTracking()
            .Include(x => x.Players)
            .Include(x => x.ChatMessages)
            .SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<bool> ResolveAsync(
        long id,
        int resolvedByUserId,
        string resolutionAction,
        CancellationToken ct = default
    )
    {
        var ticket = await db.ReportTickets.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (ticket is null || ticket.Status == ReportTicketStatus.Resolved)
            return false;

        ticket.Status = ReportTicketStatus.Resolved;
        ticket.ResolvedAt = DateTime.UtcNow;
        ticket.ResolvedByUserId = resolvedByUserId;
        ticket.ResolutionAction = resolutionAction.Trim();
        await db.SaveChangesAsync(ct);
        return true;
    }
}
