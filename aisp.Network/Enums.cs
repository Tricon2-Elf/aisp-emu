namespace aisp.Network;

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

/// <summary>
/// VCE codec header type.
/// PacketData: Normal game packet (length + payload with PacketType + body).
/// Ping: Keep-alive ping; 9-byte message, skip in packet loop.
/// Pong: Keep-alive pong response; 9-byte message, skip in packet loop.
/// Terminated: Session control terminated; 5-byte message, skip in packet loop.
/// DirectContact: DirectContact / other control; skip in normal packet handling (size not fixed in docs).
/// </summary>
public enum VceCodecHeaderType : byte
{
    PacketData = 0,
    Ping = 1,
    Pong = 2,
    Terminated = 3,
    DirectContact = 4,
}

/// <summary>
/// My room security.
/// Private: Only the owner can enter.
/// FriendsOnly: Only friends can enter.
/// CircleMembersOnly: Only circle members can enter.
/// FriendsAndCircleMembers: Friends and circle members can enter.
/// Public: Anyone can enter.
/// </summary>
public enum MyRoomSecurity : uint
{
    Private = 0,
    FriendsOnly = 1,
    CircleMembersOnly = 2,
    FriendsAndCircleMembers = 3,
    Public = 4,
}

public enum MyRoomStage : byte
{
    SixTatami = 0,
    EightTatami = 1,
    TenTatami = 2,
    TwelveTatami = 3,
}

public enum CircleAuthLevel : uint
{
    Member = 0,
    Core = 1,
    Leader = 2,
}

public enum BloodType : uint
{
    A = 1,
    B = 2,
    AB = 3,
    O = 4,
}

public enum EmotionCategory : byte
{
    Passion = 0,
    Action = 1,
    Voice = 2,
    Etc = 3,
}


/// <summary>
/// Event select type.
/// Dialogue: IF/CHL dialogue selection window (same style as client CSV if-selection-start).
/// Popup: Small centered popup (context-menu style).
/// </summary>
public enum EventSelectType : uint
{
    Dialogue = 1,
    Popup = 2,
}

[Flags]
public enum FurniturePlacementFlags : uint
{
    Floor = 0x08,
    Wall = 0x10,
    Ceiling = 0x20,
}

public enum NicotvPlaybackState : uint
{
    Closed = 0,
    Playing = 1,
    Paused = 2,
}

public enum RoboState : uint
{
    Resting = 0,
    InMyRoom = 1,
    Accompanying = 2,
}

public enum ShopPriceType : byte
{
    AiPoints = 0,
    NicoPoints = 1,
}

public enum NicotvCommentVisibility : uint
{
    Visible = 0,
    Hidden = 1,
}
