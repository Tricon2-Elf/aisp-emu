using System.Reflection;

namespace AISpace.Launcher;

internal static class LaunchVersion
{
    public static string Display { get; } =
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev";

    public static string WindowTitle => $"aisp.launch - {Display}";
}
