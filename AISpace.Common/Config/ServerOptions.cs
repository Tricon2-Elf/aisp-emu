namespace AISpace.Common.Config;

public class ServerOptions
{
    public string PublicIP { get; set; } = "127.0.0.1"; 
    public required NetworkOptions NetworkOptions { get; set; }
    public required DbOptions DbOptions { get; set; }
    public bool AuthServerEnabled { get; set; } = true;
    public bool MsgServerEnabled { get; set; } = true;
    public bool AreaServerEnabled { get; set; } = true;
}