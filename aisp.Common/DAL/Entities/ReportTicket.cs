namespace aisp.Common.DAL.Entities;

public enum ReportTicketStatus : byte
{
    Open = 0,
    Resolved = 1,
}

public sealed class ReportTicket
{
    public long Id { get; set; }
    public int ReporterUserId { get; set; }
    public string ReporterUsername { get; set; } = string.Empty;
    public int ReporterCharacterId { get; set; }
    public string ReporterCharacterName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public uint MapId { get; set; }
    public int ChannelId { get; set; }
    public string MapName { get; set; } = string.Empty;
    public ReportTicketStatus Status { get; set; } = ReportTicketStatus.Open;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolvedByUserId { get; set; }
    public string? ResolutionAction { get; set; }

    public ICollection<ReportTicketPlayer> Players { get; set; } = [];
    public ICollection<ReportTicketChatMessage> ChatMessages { get; set; } = [];
}

public sealed class ReportTicketPlayer
{
    public long Id { get; set; }
    public long ReportTicketId { get; set; }
    public ReportTicket ReportTicket { get; set; } = default!;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
}

public sealed class ReportTicketChatMessage
{
    public long Id { get; set; }
    public long ReportTicketId { get; set; }
    public ReportTicket ReportTicket { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public int CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool Rejected { get; set; }
}
