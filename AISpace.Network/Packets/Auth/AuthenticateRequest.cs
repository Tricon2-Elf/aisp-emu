using AISpace.Network;

namespace AISpace.Network.Packets.Auth;

public class AuthenticateRequest(string username, string password) : IIncomingPacket<AuthenticateRequest>
{
    public string Username = username;
    public string Password = password;

    public static AuthenticateRequest FromBytes(ReadOnlySpan<byte> data)
    {
        PacketReader reader = new(data);

        string username = reader.ReadString();
        string password = reader.ReadString();
        return new AuthenticateRequest(username, password);
    }
}
