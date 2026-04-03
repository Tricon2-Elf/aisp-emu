using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CmdExecRequest(uint messageId, string command, uint argCount, List<string> arguments) : IIncomingPacket<CmdExecRequest>
{
    const int MaxArgs = 10;
    const int ArgLength = 384;
    const int CmdLength = 96;

    public uint MessageId = messageId;
    public string Command = command;
    public uint ArgCount = argCount;

    public List<string> Arguments { get; } = arguments;

    public static CmdExecRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);

        var msgId = reader.ReadUInt();
        var cmd = reader.ReadFixedString(CmdLength, "ASCII");

        var args = new List<string>(MaxArgs);
        for (int i = 0; i < MaxArgs; i++)
        {
            string arg = reader.ReadFixedString(ArgLength, "ASCII");
            args.Add(arg);
        }
        uint argCount = reader.ReadUInt();

        return new CmdExecRequest(msgId, cmd.Trim(), argCount, args.Take((int)argCount).ToList());
    }
}
