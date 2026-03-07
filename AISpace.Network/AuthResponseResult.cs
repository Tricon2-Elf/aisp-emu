namespace AISpace.Network;

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
