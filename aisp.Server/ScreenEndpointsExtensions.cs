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
/// All of them serve the same page: it shows which route and parameters it got, implements the
/// script contract the client drives (external_nico_0.ext_*), and publishes volume and mute in
/// its title for the hook.
/// </summary>
internal static class ScreenEndpointsExtensions
{
    private static readonly string[] Routes = ["room-tv", "channel-screen", "live-watch", "screen"];

    internal static WebApplication MapScreenEndpoints(this WebApplication app)
    {
        foreach (var route in Routes)
            app.MapGet(
                "/ai-sp/" + route,
                (HttpContext context, IWebHostEnvironment environment) =>
                    ServePage(context, environment)
            );
        return app;
    }

    private static async Task<IResult> ServePage(
        HttpContext context,
        IWebHostEnvironment environment
    )
    {
        var file = environment.WebRootFileProvider.GetFileInfo("screen/screen.html");
        if (!file.Exists || file.PhysicalPath is null)
            return Results.NotFound();

        var html = await File.ReadAllTextAsync(file.PhysicalPath, context.RequestAborted);

        // The control fetches a page once per screen and the client keeps drawing that document,
        // so nothing here should ever be cached or redirected after it loaded.
        context.Response.Headers.CacheControl = "no-store";
        return Results.Content(html, "text/html; charset=utf-8");
    }
}
