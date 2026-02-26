namespace AISpace.Common.Network.Packets;

public record Packet(ClientConnection Client, PacketType Type, byte[] Data, ushort RawType);
