using System.Net;
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
                    ScreenAssignments assignments
                ) => ServePage(route, context, environment, assignments)
            );
        // Polled by the page so a changed assignment reaches screens that are already open.
        app.MapGet(
            "/ai-sp/screen-source",
            (HttpContext context, ScreenAssignments assignments) =>
            {
                var query = context.Request.Query;
                uint? mapId = uint.TryParse(query["map"], out var parsedMap) ? parsedMap : null;
                var source = assignments.Resolve(
                    query["route"].ToString(),
                    query["movieid"],
                    mapId
                );
                context.Response.Headers.CacheControl = "no-store";
                return Results.Json(new { src = source ?? "" });
            }
        );
        return app;
    }

    private static async Task<IResult> ServePage(
        string route,
        HttpContext context,
        IWebHostEnvironment environment,
        ScreenAssignments assignments
    )
    {
        var file = environment.WebRootFileProvider.GetFileInfo("screen/screen.html");
        if (!file.Exists || file.PhysicalPath is null)
            return Results.NotFound();

        var query = context.Request.Query;
        uint? mapId = uint.TryParse(query["map"], out var parsedMap) ? parsedMap : null;
        var source = assignments.Resolve(route, query["movieid"], mapId);

        // A room TV never needs the page to poll: the client re-navigates it on every assignment
        // change (movie set, channel switch, room re-entry). live-watch (the Stage) is pushed
        // too: /screen sends it notify_nicolive_reload (CmdExecHandler). A /channel-screen is a
        // town screen (confirmed on Akihabara) with neither guarantee, so it alone keeps polling.
        var noPoll = route is "room-tv" or "live-watch";
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
