namespace AISpace.Network;

public enum ClientState
{
    Init = 1,
    WaitingForHandshake = 2,
    WaitingForVersionCheck = 3,
    Connected = 4,
    ForceDisconnect = 5,
}

public enum AuthResponseResult : uint
{
    Success = 0,
    Failure = 1,
    InvalidCredentials = 2,
    AccountBanned = 3,
    ServerFull = 4,
    Maintenance = 5,
    VersionMismatch = 6,
}

public enum VceCodecHeaderType : byte
{
    PacketData = 0, // Normal game packet (length + payload with PacketType + body).
    Ping = 1, // Keep-alive ping; 9-byte message, skip in packet loop.
    Pong = 2, // Keep-alive pong response; 9-byte message, skip in packet loop.
    Terminated = 3, // Session control terminated; 5-byte message, skip in packet loop.
    DirectContact = 4, // DirectContact / other control; skip in normal packet handling (size not fixed in docs).
}
