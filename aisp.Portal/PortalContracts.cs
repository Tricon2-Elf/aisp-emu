using System.ComponentModel.DataAnnotations;
using aisp.Common.Game;

namespace aisp.Portal;

public sealed record RegisterPortalAccountRequest(
    [property: Required, RegularExpression("^[A-Za-z0-9_.-]{3,64}$")] string Username,
    [property: Required, StringLength(128, MinimumLength = 8)] string Password,
    [property: Required, Compare(nameof(RegisterPortalAccountRequest.Password))]
        string ConfirmPassword
);

public sealed record PortalLoginRequest(
    [property: Required] string Username,
    [property: Required] string Password
);

public sealed record PortalIdentityDto(int UserId, string Username, UserRole Role);

public sealed record PortalUserSummaryDto(
    int UserId,
    string Username,
    UserRole Role,
    bool IsBanned,
    DateTime CreatedAt,
    int CharacterCount,
    IReadOnlyList<string> CharacterNames
);

public sealed record PortalUserPageDto(IReadOnlyList<PortalUserSummaryDto> Users, int Total);

public sealed record PortalUserDetailDto(
    int UserId,
    string Username,
    UserRole Role,
    bool IsBanned,
    string? BanReason,
    DateTime CreatedAt,
    DateTime? LastLoggedInAt,
    DateTime? BannedAt,
    DateTime? BannedUntil,
    DateTime? KickedUntil
);

public sealed record PortalBanRequest(
    int ActorUserId,
    int? Days,
    [property: StringLength(256)] string? Reason
);

public sealed record PortalKickRequest(
    int ActorUserId,
    int? Minutes,
    [property: StringLength(256)] string? Reason
);

public sealed record PortalSetRoleRequest(int ActorUserId, UserRole Role);

public sealed record PortalActorRequest(int ActorUserId);

public sealed record PortalSetPasswordRequest(
    int ActorUserId,
    [property: Required, StringLength(128, MinimumLength = 8)] string NewPassword,
    [property: Required, Compare(nameof(PortalSetPasswordRequest.NewPassword))]
        string ConfirmPassword
);

public sealed record PortalChangePasswordRequest(
    [property: Required] string CurrentPassword,
    [property: Required, StringLength(128, MinimumLength = 8)] string NewPassword,
    [property: Required, Compare(nameof(PortalChangePasswordRequest.NewPassword))]
        string ConfirmPassword
);

public sealed record PortalDisconnectResultDto(int SessionsClosed);

public sealed record PortalItemDto(int ItemId, string Name, int Socket, int IconId, int Quantity);

public sealed record PortalCharacterEquipmentDto(
    byte SlotIndex,
    string SlotName,
    int ItemId,
    string Name,
    int Socket,
    int IconId
);

public sealed record PortalRoboEquipmentDto(
    byte SlotIndex,
    string SlotName,
    int ItemId,
    string Name,
    int Socket,
    int IconId
);

public sealed record PortalRoboDto(
    uint RoboId,
    string Name,
    uint ModelId,
    string Personality,
    byte Level,
    ulong Experience,
    ulong ExperienceToNextLevel,
    ulong StatusPoints,
    uint AvailableStatusPoints,
    IReadOnlyList<PortalRoboEquipmentDto> Equipment
);

public sealed record PortalCharacterDto(
    int CharacterId,
    string Name,
    uint ModelId,
    DateTime Birthdate,
    DateTime CreatedAt,
    DateTime? LastLoggedInAt,
    string BloodType,
    string AvatarDescription,
    string Like1,
    string LikeDescription1,
    string Like2,
    string LikeDescription2,
    string Like3,
    string LikeDescription3,
    uint CurrentMapId,
    string CurrentMapName,
    uint HomeIslandId,
    string HomeIslandName,
    string CharadollPersonality,
    IReadOnlyList<PortalItemDto> Inventory,
    IReadOnlyList<PortalCharacterEquipmentDto> Equipment,
    IReadOnlyList<PortalRoboDto> Robos
);

public sealed record PortalAccountDataDto(
    int UserId,
    string Username,
    DateTime CreatedAt,
    DateTime? LastLoggedInAt,
    long AiPoints,
    long NicoPoints,
    long StorageDeposit,
    IReadOnlyList<PortalItemDto> StorageItems,
    IReadOnlyList<PortalCharacterDto> Characters,
    string PreferredLanguage
);

public sealed record PortalChangeLanguageRequest([property: Required] string PreferredLanguage);

public sealed record PortalResetRoboRequest(
    [property: Required, StringLength(37, MinimumLength = 1)] string Name,
    [property: Required] string Personality
);

public sealed record PortalCharacterRoboSummaryDto(
    int UserId,
    IReadOnlyList<PortalCharacterRoboEntryDto> Characters
);

public sealed record PortalCharacterRoboEntryDto(
    int CharacterId,
    string CharacterName,
    int RoboCount,
    bool IsOnline,
    string Location
);

public sealed record PortalUserIdsRequest(
    [property: Required, MinLength(1)] IReadOnlyList<int> UserIds
);

public sealed record PortalErrorDto(string Error);

public sealed record PortalChatMessageDto(
    long Id,
    string Kind,
    int CharacterId,
    string CharacterName,
    string Message,
    int? CircleId,
    uint? MapId,
    int? ChannelId,
    bool Rejected,
    DateTime CreatedAt
);

public sealed record PortalChatPageDto(IReadOnlyList<PortalChatMessageDto> Messages, int Total);
