using System.Security.Cryptography;
using System.Text;
using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Network;
using AISpace.Common.Network.Packets;
using AISpace.Common.Network.Packets.World;
using NLog;

namespace AISpace.Auth.Server;

internal class AuthServer
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly TcpServer server;
    private readonly UserRepository _userRepo;
    private readonly WorldRepository _worldRepo;
    private readonly MainContext context;

    public AuthServer(int port)
    {
        server = new TcpServer("0.0.0.0", port, false);
        context = new MainContext();
        context.Database.EnsureCreated();
        _userRepo = new UserRepository(context);
        _worldRepo = new WorldRepository(context);
    }
    public async void Start()
    {
        _logger.Info("Starting Auth server");

        _logger.Info("Starting Database connection");
        await _userRepo.AddUserAsync("hideki@animetoshokan.org", "password");
        await _worldRepo.AddWorldAsync("test", "test2");
        _logger.Info("Starting TCP Server");
        server.Start();

        _logger.Info("Starting Main Loop");
        await foreach (var packet in server.PacketReader.ReadAllAsync())
        {
            ClientContext Client = packet.Client;
            string ClientID = packet.Client.Id.ToString();
            var payload = packet.Data;
            var type = packet.Type;
            if (packet.Type != PacketType.Ping)
                _logger.Info($"Client: Auth {packet.Type}");
            switch (type)
            {
                case PacketType.Ping:
                    var ping = PingRequest.FromBytes(payload);
                    _ = Client.SendAsync(PacketType.Ping, ping.ToBytes());
                    break;
                case PacketType.Auth_VersionCheckRequest:
                    var req = VersionCheckRequest.FromBytes(payload);
                    _logger.Info($"Client: {ClientID} Version: {packet.Client.Version}");
                    var resp = new VersionCheckResponse(0, req.Major, req.Minor, req.Version);
                    _ = Client.SendAsync(PacketType.Auth_VersionCheckResponse, resp.ToBytes());
                    break;
                case PacketType.AuthenticateRequest:
                    var AuthReq = AuthenticateRequest.FromBytes(payload);
                    _logger.Info($"Username: '{AuthReq.Username}', Password: {AuthReq.Password}");
                    //bool valid = await repo.ValidateCredentialsAsync(req.Username, req.Password);
                    uint userID = 31874;
                    var AuthResp = new AuthenticateResponse(userID);
                    _ = Client.SendAsync(PacketType.AuthenticateResponse, AuthResp.ToBytes());
                    break;
                case PacketType.Auth_WorldListRequest:
                    //Get Worlds from Repo
                    var worlds = await _worldRepo.GetAllWorldsAsync();
                    var worldListResponse = new WorldListResponse(0, worlds);
                    _ = Client.SendAsync(PacketType.Auth_WorldListResponse, worldListResponse.ToBytes());
                    break;
                case PacketType.Auth_WorldSelectRequest:
                    //Get World from Repo by ID
                    var WorldSelectReq = WorldSelectRequest.FromBytes(payload);
                    var selectedWorldID = WorldSelectReq.WorldID;
                    string otp = GenerateOTP();
                    //Need to insert the otp into UserSessions
                    _logger.Info($"World Selected: {selectedWorldID}");
                    var WorldSelectResp = new WorldSelectResponse(0, "127.0.0.1", 50052, otp);
                    _ = Client.SendAsync(PacketType.Auth_WorldSelectResponse, WorldSelectResp.ToBytes());
                    break;
                default:
                    _logger.Error($"Unknown packet type: {packet.RawType:X4}");
                    break;
            }
        }
    }

    private static string GenerateOTP()
    {
        byte[] seed = Guid.NewGuid().ToByteArray();
        byte[] hash = SHA256.HashData(seed);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash)
            sb.Append(b.ToString("x2"));
        string opt = sb.ToString(0, 20);
        return opt;
    }
}
