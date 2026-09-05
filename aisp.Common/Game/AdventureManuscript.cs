using System.Text;

namespace aisp.Common.Game;

/// <summary>What upload.php learned from the two text parts, or why it refused them.</summary>
public sealed record AdventureManuscriptCheck(bool Ok, string Error, int Pages)
{
    public static AdventureManuscriptCheck Refuse(string error) => new(false, error, 0);
}

/// <summary>
/// Sanity checks for an uploaded drama manuscript: the command script (drama_N.csv re-encoded to UTF-8) and the
/// actor table (datalist_N.txt). The server never runs them, but everything it stores is handed to every buyer's
/// client to pack and play, so it insists on the shape the editor produces: valid UTF-8 text, at least one
/// PAGEHEADER sheet with the first one opening with CHANGEMAP, and an [ACTOR] table.
/// </summary>
public static class AdventureManuscript
{
    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true
    );

    public static AdventureManuscriptCheck Check(byte[] script, byte[]? datalist)
    {
        if (!TryDecode(script, out var scriptText))
            return AdventureManuscriptCheck.Refuse("script is not UTF-8 text");
        if (datalist is { Length: > 0 } && !TryDecode(datalist, out _))
            return AdventureManuscriptCheck.Refuse("datalist is not UTF-8 text");
        if (datalist is { Length: > 0 } && !DecodeLines(datalist).Any(l => l == "[ACTOR]"))
            return AdventureManuscriptCheck.Refuse("datalist has no [ACTOR] table");

        var pages = 0;
        var expectingChangeMap = false;
        foreach (var raw in scriptText.Split('\n'))
        {
            var line = raw.TrimEnd('\r').TrimStart();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("//"))
                continue;
            var token = line.Split(',', 2)[0].Trim();
            if (token == "PAGEHEADER")
            {
                pages++;
                // Playback enters through the first sheet, which must open with CHANGEMAP to build the stage;
                // later sheets are jump targets and the official discs open them with anything.
                expectingChangeMap = pages == 1;
                continue;
            }
            if (expectingChangeMap)
            {
                if (token != "CHANGEMAP")
                    return AdventureManuscriptCheck.Refuse(
                        "the first sheet does not start with CHANGEMAP"
                    );
                expectingChangeMap = false;
            }
            else if (pages == 0)
                return AdventureManuscriptCheck.Refuse("commands before the first PAGEHEADER");
        }
        if (pages == 0)
            return AdventureManuscriptCheck.Refuse("script has no PAGEHEADER");
        if (expectingChangeMap)
            return AdventureManuscriptCheck.Refuse("the first sheet has no commands");
        return new AdventureManuscriptCheck(true, "", pages);
    }

    private static bool TryDecode(byte[] bytes, out string text)
    {
        try
        {
            text = StrictUtf8.GetString(AdventureScriptPacker.StripUtf8Bom(bytes));
        }
        catch (DecoderFallbackException)
        {
            text = "";
            return false;
        }
        return !text.Any(c => c < ' ' && c is not '\r' and not '\n' and not '\t');
    }

    private static IEnumerable<string> DecodeLines(byte[] bytes) =>
        StrictUtf8
            .GetString(AdventureScriptPacker.StripUtf8Bom(bytes))
            .Split('\n')
            .Select(l => l.Trim());
}
