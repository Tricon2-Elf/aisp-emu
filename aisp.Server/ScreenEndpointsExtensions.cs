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
                    INicotvRepository nicotvRepository,
                    ILoggerFactory loggers
                ) => ServePage(route, context, environment, assignments, nicotvRepository, loggers)
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
                uint? requestTvId = uint.TryParse(query["tvid"], out var parsedTvId)
                    ? parsedTvId
                    : null;
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
                    new
                    {
                        applied,
                        src = assignments.Resolve(route, movieId, mapId, channel, requestTvId)
                            ?? "",
                    }
                );
            }
        );
        // Polled by the page so a changed assignment reaches screens that are already open.
        app.MapGet(
            "/ai-sp/screen-source",
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
                uint? requestTvId = uint.TryParse(query["tvid"], out var parsedTvId)
                    ? parsedTvId
                    : null;
                // TEMPORARY: log the raw query so we can see exactly what the client is polling
                // with. Remove once the tvid round-trip is confirmed end to end.
                loggers
                    .CreateLogger("aisp.Server.ScreenEvents")
                    .LogInformation(
                        "screen-source poll {Query}",
                        context.Request.QueryString.Value
                    );
                var (route, movieId) = await ResolveRoomTvAsync(
                    query["route"].ToString(),
                    query,
                    nicotvRepository,
                    context.RequestAborted
                );
                var source = assignments.Resolve(route, movieId, mapId, channel, requestTvId);
                context.Response.Headers.CacheControl = "no-store";
                return Results.Json(new { src = source ?? "" });
            }
        );
        return app;
    }

    /// <summary>
    /// A room TV may identify the furniture it's for as an n: tag on movieid (the tag the server
    /// appends to its own Notify* broadcasts and get-info/open responses): when it does, the
    /// database's own movie and channel state for that Nicotv is authoritative over whatever the
    /// client's URL still says, and the route becomes room-tv so
    /// <see cref="ScreenAssignments.Resolve"/> plays it.
    ///
    /// channel-screen's own tvid= query parameter is a different thing: the client's own word,
    /// not ours. It is the channel number itself (0 through 4, and 0 is a real channel, not "no
    /// channel"), sent when a room TV is tuned to a channel rather than a typed movie, including
    /// on room re-entry. It is only a channel reference on a MyRoom map: a town map's own screens
    /// (confirmed on Akihabara) send a tvid= too, and treating that as a channel would override
    /// the town screen's own /screen assignment. So only a room TV's tvid= indirects through
    /// channel:&lt;n&gt;, the same way the database lookup above does for a channel-tuned Nicotv
    /// row.
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
            // A movie id and a channel are exclusive on a Nicotv row (see NicotvRepository), so
            // at most one of these is ever set.
            var content =
                nicotv is null ? null
                : !string.IsNullOrEmpty(nicotv.MovieId) ? nicotv.MovieId
                : nicotv.ChannelId != 0 ? $"channel:{nicotv.ChannelId}"
                : null;
            return ("room-tv", content is null ? null : $"{content} n:{nicotvId}");
        }

        if (
            route == "channel-screen"
            && uint.TryParse(query["map"], out var mapId)
            && MyRoomInfo.IsMyRoomMap(mapId)
            && uint.TryParse(query["tvid"], out var channelNumber)
        )
            return ("room-tv", $"channel:{channelNumber}");

        return (route, query["movieid"]);
    }

    private static async Task<IResult> ServePage(
        string route,
        HttpContext context,
        IWebHostEnvironment environment,
        ScreenAssignments assignments,
        INicotvRepository nicotvRepository,
        ILoggerFactory loggers
    )
    {
        var file = environment.WebRootFileProvider.GetFileInfo("screen/screen.html");
        if (!file.Exists || file.PhysicalPath is null)
            return Results.NotFound();

        // TEMPORARY: log the raw query of the page navigation itself. Remove once the tvid
        // round-trip is confirmed end to end.
        loggers
            .CreateLogger("aisp.Server.ScreenEvents")
            .LogInformation(
                "screen page GET /ai-sp/{Route}{Query}",
                route,
                context.Request.QueryString.Value
            );

        var query = context.Request.Query;
        uint? mapId = uint.TryParse(query["map"], out var parsedMap) ? parsedMap : null;
        var channel = int.TryParse(query["ch"], out var parsedChannel) ? parsedChannel : 0;
        uint? requestTvId = uint.TryParse(query["tvid"], out var parsedTvId) ? parsedTvId : null;
        var (effectiveRoute, movieId) = await ResolveRoomTvAsync(
            route,
            query,
            nicotvRepository,
            context.RequestAborted
        );
        var source = assignments.Resolve(effectiveRoute, movieId, mapId, channel, requestTvId);

        // A genuine room TV (whether reached as /room-tv, or as /channel-screen resolved onto
        // one by a MyRoom map above) never needs the page to poll: the client re-navigates it on
        // every assignment change (movie set, channel switch, room re-entry). live-watch (the
        // Stage) is pushed too: both /screen and /channel on a map it is bound to send it
        // notify_nicolive_reload (CmdExecHandler). A /channel-screen that did not resolve to a
        // room TV is a town screen (confirmed on Akihabara) with neither guarantee, so it alone
        // keeps polling.
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
