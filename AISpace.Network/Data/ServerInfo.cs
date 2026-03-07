namespace AISpace.Network.Data;

public class ServerInfo(string ip, ushort port)
{
    public ushort Port = port;
    public string IP = ip;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Port);
        writer.WriteFixedString(IP, 65, "ASCII");
        return writer.ToBytes();
    }
}
