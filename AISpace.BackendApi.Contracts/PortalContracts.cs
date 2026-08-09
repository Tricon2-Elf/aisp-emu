using System.ComponentModel.DataAnnotations;

namespace AISpace.BackendApi.Contracts;

public sealed record RegisterPortalAccountRequest(
    [property: Required, RegularExpression("^[A-Za-z0-9_.-]{3,64}$")] string Username,
    [property: Required, StringLength(128, MinimumLength = 8)] string Password,
    [property: Required, Compare(nameof(RegisterPortalAccountRequest.Password))] string ConfirmPassword
);

public sealed record PortalLoginRequest(
    [property: Required] string Username,
    [property: Required] string Password
);

public sealed record PortalIdentityDto(int UserId, string Username);

public sealed record PortalUserSummaryDto(
    int UserId,
    string Username,
    bool IsBanned,
    DateTime CreatedAt,
    int CharacterCount,
    IReadOnlyList<string> CharacterNames
);

public sealed record PortalUserPageDto(IReadOnlyList<PortalUserSummaryDto> Users, int Total);

public sealed record PortalUserDetailDto(
    int UserId,
    string Username,
    bool IsBanned,
    string? BanReason,
    DateTime CreatedAt,
    DateTime? BannedAt
);

public sealed record PortalBanRequest([property: StringLength(256)] string? Reason);

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

public sealed record PortalRoboEquipmentDto(byte SlotIndex, uint ItemId, uint Socket);

public sealed record PortalRoboDto(
    uint RoboId,
    string Name,
    uint ModelId,
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
    uint CurrentMapId,
    string CurrentMapName,
    uint HomeIslandId,
    string HomeIslandName,
    IReadOnlyList<PortalItemDto> Inventory,
    IReadOnlyList<PortalCharacterEquipmentDto> Equipment,
    IReadOnlyList<PortalRoboDto> Robos
);

public sealed record PortalAccountDataDto(
    int UserId,
    string Username,
    long AiPoints,
    long NicoPoints,
    long StorageDeposit,
    IReadOnlyList<PortalItemDto> StorageItems,
    IReadOnlyList<PortalCharacterDto> Characters
);

public sealed record PortalCharacterRoboSummaryDto(
    int UserId,
    IReadOnlyList<PortalCharacterRoboEntryDto> Characters
);

public sealed record PortalCharacterRoboEntryDto(int CharacterId, string CharacterName, int RoboCount);

public sealed record PortalUserIdsRequest([property: Required, MinLength(1)] IReadOnlyList<int> UserIds);

public sealed record PortalErrorDto(string Error);
