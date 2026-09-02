namespace aisp.Common.DAL.Entities;

/// <summary>
/// A user-made drama (adventure) work registered by the client's manuscript editor. The manuscript itself lives
/// only on the client (user/&lt;uid&gt;/&lt;slot&gt;/work/drama); the server keeps the id, the sheet count and the
/// upload state. WorkId is per account and must never be reused: the client creates the local files for whatever id
/// recv_adventure_work_create_r returns and overwrites an existing work under that id.
/// </summary>
public sealed class AdventureWork
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = default!;
    public int CharacterId { get; set; }
    public int WorkId { get; set; }
    public int Sheets { get; set; }
    public bool Uploaded { get; set; }
    public DateTime CreatedAt { get; set; }
}
