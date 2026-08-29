using aisp.Common.DAL.Entities;

namespace aisp.Common.Game;

/// <summary>
/// Maps home island + Charadoll personality to the default doll model and hairstyle
/// used at create time (and Portal reset).
/// </summary>
public static class CharadollAppearance
{
    public static readonly Appearance Default = new(1002011, 10930010);

    public readonly record struct Appearance(uint ModelId, uint Hairstyle);

    /// <summary>
    /// Resolves model/hairstyle for a concrete Active or Quiet personality.
    /// Callers that receive <see cref="CharadollPersonality.None"/> should randomise
    /// Active/Quiet first (create-info path only).
    /// </summary>
    public static Appearance Resolve(uint homeIslandId, CharadollPersonality personality) =>
        (homeIslandId, personality) switch
        {
            // Da Capo — 朝倉由夢 / 白河ななか (hair is baked into the model)
            (1, CharadollPersonality.Quiet) => new Appearance(2012020, 0),
            (1, CharadollPersonality.Active) => new Appearance(2012030, 0),
            // Clannad — 坂上智代 / 藤林杏
            (2, CharadollPersonality.Quiet) => new Appearance(2022030, 0),
            (2, CharadollPersonality.Active) => new Appearance(2022020, 0),
            // Shuffle! — ネリネ / 芙蓉楓
            (3, CharadollPersonality.Quiet) => new Appearance(2032020, 0),
            (3, CharadollPersonality.Active) => new Appearance(2032030, 0),
            _ => Default,
        };
}
