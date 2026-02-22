using System.Text;

namespace AISpace.Common.Network.Packets.Auth;

public class AuthenticateRequest(string username, string password) : IPacket<AuthenticateRequest>
{
    public string Username = username;
    public string Password = password;

    public static AuthenticateRequest FromBytes(ReadOnlySpan<byte> data)
    {
        PacketReader reader = new(data);

        // ВАЖНО: ReadString читает до null-терминатора.
        // Если клиент шлет мусор после строки, ReadString должен остановиться на \0.
        string username = reader.ReadString();
        string password = reader.ReadString();
        
        return new AuthenticateRequest(username, password);
    }

    public byte[] ToBytes() => throw new NotImplementedException();
}