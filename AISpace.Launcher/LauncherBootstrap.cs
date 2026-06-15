using Avalonia.Controls;
using Avalonia.Platform;

namespace AISpace.Launcher;

internal static class LauncherBootstrap
{
    public static LauncherSettings Settings { get; private set; } = null!;

    public static void Initialize() => Settings = LauncherSettings.LoadOrCreate();

    public static void ConfigureWebViewEnvironment(NativeWebView webView)
    {
        webView.EnvironmentRequested += (_, args) =>
        {
            var dataFolder = GetWebViewDataDirectory();
            switch (args)
            {
                case WindowsWebView2EnvironmentRequestedEventArgs webView2:
                    webView2.UserDataFolder = dataFolder;
                    break;
                case LinuxWpeWebViewEnvironmentRequestedEventArgs wpe:
                    wpe.DataDirectory = dataFolder;
                    wpe.CacheDirectory = Path.Combine(dataFolder, "cache");
                    break;
            }
        };
    }

    private static string GetWebViewDataDirectory()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AISpace", "Launcher", "WebView2");
        Directory.CreateDirectory(dir);
        return dir;
    }
}
