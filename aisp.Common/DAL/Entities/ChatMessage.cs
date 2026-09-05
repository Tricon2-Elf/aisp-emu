namespace aisp.Common.DAL.Entities;

public enum ChatLogKind : byte
{
    Public = 0,
    Circle = 1,
    Placard = 2,
}

public sealed class ChatMessage
{
    public long Id { get; set; }
    public ChatLogKind Kind { get; set; }
    public int UserId { get; set; }
    public int CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public uint DistId { get; set; }
    public uint BalloonId { get; set; }
    public int? CircleId { get; set; }
    public uint? MapId { get; set; }
    public int? ChannelId { get; set; }
    public bool Rejected { get; set; }
    public DateTime CreatedAt { get; set; }
}
