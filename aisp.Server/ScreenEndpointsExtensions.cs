using System.Net;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace aisp.Server;

/// <summary>
/// Pages for the in-game screens. The client's displays are IE controls navigated to URLs it
/// builds from its own templates; the launcher's hook rewrites those to these paths, keeping the
/// distinction between them, passes the client's parameters along as the query string, and adds
/// the map and channel the client is on (map=, ch=), which the URL itself does not carry:
///   /ai-sp/room-tv?movieid=...            a TV given a movie id (動画指定)
///   /ai-sp/channel-screen?tvid=..&chid=.. a TV or town screen on a channel
///   /ai-sp/live-watch?liveid=...          the Nico Live billboard
///   /ai-sp/screen?url=...                 anything else the client asked aisp.jp for
///   /ai-sp/screen-source?route=..&map=..  JSON {src} the page polls for changes
/// All of them serve the same page: it shows which route and parameters it got, implements the
/// script contract the client drives (external_nico_0.ext_*), and publishes volume, mute and the
/// source to play in its title for the hook. The source is decided here from ScreenAssignments.
/// </summary>
internal static class ScreenEndpointsExtensions
{
    private static readonly string[] Routes = ["room-tv", "channel-screen", "live-watch", "screen"];
    private const string TitleMarker = "<title>aisp:vol=100;mute=0</title>";

    internal static WebApplication MapScreenEndpoints(this WebApplication app)
    {
        foreach (var route in Routes)
            app.MapGet(
                "/ai-sp/" + route,
                (
                    HttpContext context,
                    IWebHostEnvironment environment,
                    ScreenAssignments assignments,
                    INicotvRepository nicotvRepository
                ) => ServePage(route, context, environment, assignments, nicotvRepository)
            );
        // Moves a video's shared timeline for everyone; the reply is the new source line. The
        // page does not use it yet: the client calls its ext_play/ext_pause around its own
        // lifecycle (leaving a map, arriving), which would pause the video for everyone.
        app.MapPost(
            "/ai-sp/screen-control",
            async (
                HttpContext context,
                ScreenAssignments assignments,
                INicotvRepository nicotvRepository,
                ILoggerFactory loggers
            ) =>
            {
                var query = context.Request.Query;
                uint? mapId = uint.TryParse(query["map"], out var parsedMap) ? parsedMap : null;
                var channel = int.TryParse(query["ch"], out var parsedChannel) ? parsedChannel : 0;
                var action = query["action"].ToString();
                var (route, movieId) = await ResolveRoomTvAsync(
                    query["route"].ToString(),
                    query,
                    nicotvRepository,
                    context.RequestAborted
                );
                var applied =
                    route == "room-tv" && !string.IsNullOrEmpty(movieId)
                        ? assignments.ControlMovie(mapId ?? 0, channel, movieId, action)
                        : mapId is { } m && assignments.Control(m, action);
                loggers
                    .CreateLogger("aisp.Server.ScreenEvents")
                    .LogInformation(
                        "screen control {Action} on route {Route} map {Map} movie {Movie} from {Remote}: {Applied}",
                        action,
                        route,
                        mapId,
                        movieId,
                        context.Connection.RemoteIpAddress,
                        applied ? "applied" : "ignored"
                    );
                context.Response.Headers.CacheControl = "no-store";
                return Results.Json(
                    new { applied, src = assignments.Resolve(route, movieId, mapId, channel) ?? "" }
                );
            }
        );
        // Polled by the page so a changed assignment reaches screens that are already open.
        app.MapGet(
            "/ai-sp/screen-source",
            async (
                HttpContext context,
                ScreenAssignments assignments,
                INicotvRepository nicotvRepository
            ) =>
            {
                var query = context.Request.Query;
                uint? mapId = uint.TryParse(query["map"], out var parsedMap) ? parsedMap : null;
                var channel = int.TryParse(query["ch"], out var parsedChannel) ? parsedChannel : 0;
                var (route, movieId) = await ResolveRoomTvAsync(
                    query["route"].ToString(),
                    query,
                    nicotvRepository,
                    context.RequestAborted
                );
                var source = assignments.Resolve(route, movieId, mapId, channel);
                context.Response.Headers.CacheControl = "no-store";
                return Results.Json(new { src = source ?? "" });
            }
        );
        return app;
    }

    /// <summary>
    /// A room TV may identify the furniture it's for as an n: tag on movieid (the tag the server
    /// appends to its own Notify* broadcasts and get-info/open responses): when it does,
    /// the database's own movie state for that Nicotv is authoritative over whatever the
    /// client's URL still says, and the route becomes room-tv so
    /// <see cref="ScreenAssignments.Resolve"/> plays it.
    /// </summary>
    private static async Task<(string Route, string? MovieId)> ResolveRoomTvAsync(
        string route,
        IQueryCollection query,
        INicotvRepository nicotvRepository,
        CancellationToken ct
    )
    {
        if (ScreenAssignments.TryGetNicotvId(query["movieid"], out var nicotvId))
        {
            var nicotv = await nicotvRepository.GetByIdAsync(nicotvId, ct);
            var content = string.IsNullOrEmpty(nicotv?.MovieId) ? null : nicotv.MovieId;
            return ("room-tv", content is null ? null : $"{content} n:{nicotvId}");
        }

        return (route, query["movieid"]);
    }

    private static async Task<IResult> ServePage(
        string route,
        HttpContext context,
        IWebHostEnvironment environment,
        ScreenAssignments assignments,
        INicotvRepository nicotvRepository
    )
    {
        var file = environment.WebRootFileProvider.GetFileInfo("screen/screen.html");
        if (!file.Exists || file.PhysicalPath is null)
            return Results.NotFound();

        var query = context.Request.Query;
        uint? mapId = uint.TryParse(query["map"], out var parsedMap) ? parsedMap : null;
        var channel = int.TryParse(query["ch"], out var parsedChannel) ? parsedChannel : 0;
        var (effectiveRoute, movieId) = await ResolveRoomTvAsync(
            route,
            query,
            nicotvRepository,
            context.RequestAborted
        );
        var source = assignments.Resolve(effectiveRoute, movieId, mapId, channel);

        // A room TV never needs the page to poll: the client re-navigates it on every assignment
        // change (movie set, channel switch, room re-entry). live-watch (the Stage) is pushed
        // too: /screen sends it notify_nicolive_reload (CmdExecHandler). A /channel-screen is a
        // town screen (confirmed on Akihabara) with neither guarantee, so it alone keeps polling.
        var noPoll = effectiveRoute is "room-tv" or "live-watch";
        var titleSuffix =
            (noPoll ? ";nopoll=1" : "")
            + (source is not null ? $";src={WebUtility.HtmlEncode(source)}" : "");

        var html = await File.ReadAllTextAsync(file.PhysicalPath, context.RequestAborted);
        if (titleSuffix.Length > 0)
            html = html.Replace(TitleMarker, $"<title>aisp:vol=100;mute=0{titleSuffix}</title>");

        // The control fetches a page once per screen and the client keeps drawing that document,
        // so nothing here should ever be cached or redirected after it loaded.
        context.Response.Headers.CacheControl = "no-store";
        return Results.Content(html, "text/html; charset=utf-8");
    }
}
