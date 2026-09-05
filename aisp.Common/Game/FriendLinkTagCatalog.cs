using System.Text;
using System.Text.Json;
using aisp.Common.DAL.Entities;
using aisp.Common.Localisation;
using aisp.Network.Data;

namespace aisp.Common.Game;

public enum FriendLinkPlacardTagType : uint
{
    Free = 0,
    Questionnaire = 1,
}

/// <summary>Defines server-provided Friend Link tags and resolves the client's type/slot selection.</summary>
public static class FriendLinkTagCatalog
{
    private static readonly Lazy<FriendLinkTagSeed> Seed = new(LoadSeed);

    public static IReadOnlyList<FriendLinkTagData> FreeTags => Seed.Value.FreeTags;

    public static IReadOnlyList<FriendLinkTagData> QuestionnaireTags =>
        Seed.Value.QuestionnaireTags;

    private static FriendLinkTagSeed LoadSeed()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "seedData", "friendLinkTags.json");
        using var stream = File.OpenRead(path);
        var file =
            JsonSerializer.Deserialize<FriendLinkTagSeedFile>(stream, SeedJson.Options)
            ?? throw new InvalidDataException($"Friend Link tag seed file is empty: {path}");

        return new FriendLinkTagSeed(
            ValidateTags(file.FreeTags, 100, "free", path),
            ValidateTags(file.QuestionnaireTags, 5, "questionnaire", path)
        );
    }

    private static IReadOnlyList<FriendLinkTagData> ValidateTags(
        IReadOnlyList<FriendLinkTagData>? tags,
        int maximumCount,
        string kind,
        string path
    )
    {
        tags ??= [];
        if (tags.Count > maximumCount)
            throw new InvalidDataException(
                $"Friend Link {kind} tag count exceeds {maximumCount} in {path}"
            );

        var ids = new HashSet<uint>();
        foreach (var tag in tags)
        {
            if (tag.Id == 0 || !ids.Add(tag.Id))
                throw new InvalidDataException(
                    $"Friend Link {kind} tag IDs must be unique and non-zero in {path}"
                );
            if (
                string.IsNullOrWhiteSpace(tag.Name)
                || Encoding.UTF8.GetByteCount(tag.Name) >= FriendLinkTagData.NameBytes
            )
                throw new InvalidDataException(
                    $"Friend Link {kind} tag {tag.Id} has an empty or oversized name in {path}"
                );
        }

        return tags.ToArray();
    }

    public static uint GetFreeTagId(string name, uint slot)
    {
        var catalogTag = FreeTags.FirstOrDefault(x =>
            string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)
        );
        if (catalogTag is not null)
            return catalogTag.Id;
        if (string.IsNullOrWhiteSpace(name))
            return slot + 1;

        // Arbitrary tags need an identity independent of the owner's local slot.
        var normalized = name.Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();
        var hash = 2166136261u;
        foreach (var value in Encoding.UTF8.GetBytes(normalized))
        {
            hash ^= value;
            hash = unchecked(hash * 16777619u);
        }
        return 0x4000_0000u | (hash & 0x3FFF_FFFFu);
    }

    public static bool TryResolvePlacementTag(
        uint type,
        uint slot,
        IReadOnlyList<FriendLinkTag> savedFreeTags,
        out FriendLinkTagData tag
    )
    {
        tag = default!;
        switch ((FriendLinkPlacardTagType)type)
        {
            case FriendLinkPlacardTagType.Free:
                var saved = savedFreeTags.FirstOrDefault(x => x.Slot == slot);
                if (saved is not null && !string.IsNullOrWhiteSpace(saved.Name))
                {
                    tag = new FriendLinkTagData(GetFreeTagId(saved.Name, saved.Slot), saved.Name);
                    return true;
                }
                if (slot < (uint)FreeTags.Count)
                {
                    tag = FreeTags[(int)slot];
                    return true;
                }
                return false;

            case FriendLinkPlacardTagType.Questionnaire:
                if (slot < (uint)QuestionnaireTags.Count)
                {
                    tag = QuestionnaireTags[(int)slot];
                    return true;
                }
                return false;

            default:
                return false;
        }
    }

    private sealed record FriendLinkTagSeed(
        IReadOnlyList<FriendLinkTagData> FreeTags,
        IReadOnlyList<FriendLinkTagData> QuestionnaireTags
    );

    private sealed class FriendLinkTagSeedFile
    {
        public FriendLinkTagData[] FreeTags { get; init; } = [];
        public FriendLinkTagData[] QuestionnaireTags { get; init; } = [];
    }
}
