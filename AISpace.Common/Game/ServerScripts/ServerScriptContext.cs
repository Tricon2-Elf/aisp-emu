using AISpace.Common.DAL.Entities;

namespace AISpace.Common.Game.ServerScripts;

public sealed class ServerScriptContext
{
    public required Npc Npc { get; init; }

    /// <summary>Set when chaining into <see cref="ServerEvents.Keys.ShinjuCharadoll"/> after island pick.</summary>
    public uint? PendingIslandId { get; init; }
}
