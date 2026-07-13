namespace AISpace.Common.Game.ServerScripts;

public sealed class ServerScriptState
{
    public required string EventKey { get; init; }
    public required string Step { get; set; }
    public Dictionary<string, object> Data { get; } = [];
}
