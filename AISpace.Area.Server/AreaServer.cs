using Microsoft.Extensions.Hosting;
using NLog;

namespace AISpace.Area.Server;

public class AreaServer(int port = 50054)
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    //private readonly TcpListenerService tcpServer = new("0.0.0.0", port, false);

    public async void Start()
    {
        var builder = Host.CreateApplicationBuilder();
        _logger.Info("Starting Area server");
    }
}
