namespace aisp.Common.DAL.Entities;

public enum MailDestinationType : byte
{
    Character = 0,
    Circle = 1,
}

public sealed class MailMessage
{
    public long Id { get; set; }
    public int SenderCharacterId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public MailDestinationType DestinationType { get; set; }
    public int DestinationId { get; set; }
    public string DestinationName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool SenderDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<MailRecipient> Recipients { get; set; } = [];
}

public sealed class MailRecipient
{
    public long MailMessageId { get; set; }
    public MailMessage MailMessage { get; set; } = default!;
    public int CharacterId { get; set; }
    public byte InboxType { get; set; }
    public bool IsRead { get; set; }
    public bool IsProtected { get; set; }
    public bool IsDeleted { get; set; }
}
