using System.Collections.Generic;
using AISpace.Common.Network;
using AISpace.Common.Network.Handlers;
using NLog;

namespace AISpace.Msg.Server;

public class MsgServer(int port = 50052)
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    //private readonly TcpListenerService tcpServer = new("0.0.0.0", port, false);


    public async void Start()
    {
        _logger.Info("Starting Msg server");
        //tcpServer.Start();
    }
}
