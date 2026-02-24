using AISpace.Common.Game;

namespace AISpace.Server;

public static class ServerApiEndpoints
{
    public static void MapServerApi(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithTags("Server API");

        group
            .MapGet(
                "/servers/status",
                (SharedState state) =>
                {
                    return Results.Ok(new { Area = new { ClientCount = state.AreaClients.Count }, Msg = new { ClientCount = state.MsgClients.Count } });
                }
            )
            .WithName("GetServerStatus")
            .WithDescription("Get Area and Msg server status and client counts.");

        group
            .MapGet(
                "/area/clients",
                (SharedState state) =>
                {
                    var clients = state
                        .AreaClients.Values.Select(c => new
                        {
                            c.Id,
                            RemoteEndPoint = c.RemoteEndPoint?.ToString(),
                            c.CurrentState,
                            c.Connected,
                            UserId = c.User?.Id,
                        })
                        .ToList();
                    return Results.Ok(clients);
                }
            )
            .WithName("GetAreaClients")
            .WithDescription("List connected Area server clients.");

        group
            .MapGet(
                "/msg/clients",
                (SharedState state) =>
                {
                    var clients = state
                        .MsgClients.Values.Select(c => new
                        {
                            c.Id,
                            RemoteEndPoint = c.RemoteEndPoint?.ToString(),
                            c.CurrentState,
                            c.Connected,
                            UserId = c.User?.Id,
                        })
                        .ToList();
                    return Results.Ok(clients);
                }
            )
            .WithName("GetMsgClients")
            .WithDescription("List connected Msg server clients.");
    }
}
