namespace AISpace.Launcher;

public static class ConnectionFile
{
    public const string FileName = "connection.txt";

    public static void Write(string gameDirectory, EnvironmentSettings settings)
    {
        var lines = new[] { $"1,{settings.AuthHost},# server ip", $"2,{settings.AuthPort},# server port", $"3,{settings.BypassNicoLogin},# bypass nico login", $"4,{settings.DownloadHost},# download ip", $"5,{settings.DownloadPath},# download path", $"6,{settings.UploadHost},# upload ip", $"7,{settings.UploadPath},# upload path" };

        File.WriteAllText(Path.Combine(gameDirectory, FileName), string.Join("\r\n", lines) + "\r\n");
    }
}
