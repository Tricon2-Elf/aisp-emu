using System.Text;

namespace AISpace.Network;

internal static class PacketEncoding
{
    private static readonly object Sync = new();
    private static bool _registered;

    public static Encoding GetEncoding(string name)
    {
        EnsureRegistered();
        return Encoding.GetEncoding(name);
    }

    private static void EnsureRegistered()
    {
        if (_registered)
            return;

        lock (Sync)
        {
            if (_registered)
                return;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _registered = true;
        }
    }
}
