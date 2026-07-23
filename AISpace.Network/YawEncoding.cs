namespace AISpace.Network;

/// <summary>
/// Character / maplink yaw on the wire is half-degrees: the client builds a Y-axis quaternion
/// whose rotation angle is <c>2 * wireByte</c> degrees. Authoring and session state use full degrees.
/// </summary>
public static class YawEncoding
{
    /// <summary>Convert authored degrees (any int) to the unsigned wire byte the client expects.</summary>
    public static byte ToWireByte(int degrees)
    {
        var normalized = NormalizeDegrees(degrees);
        return (byte)(normalized / 2);
    }

    /// <summary>Same as <see cref="ToWireByte"/>, re-interpreted as a signed byte for sbyte packet fields.</summary>
    public static sbyte ToWireSByte(int degrees) => unchecked((sbyte)ToWireByte(degrees));

    /// <summary>Convert a wire yaw byte back to degrees (0–358, even steps).</summary>
    public static int FromWireByte(byte wire) => NormalizeDegrees(wire * 2);

    /// <summary>Convert a signed wire yaw byte back to degrees.</summary>
    public static int FromWireSByte(sbyte wire) => FromWireByte(unchecked((byte)wire));

    /// <summary>Normalize to <c>[0, 360)</c>.</summary>
    public static int NormalizeDegrees(int degrees)
    {
        var normalized = degrees % 360;
        if (normalized < 0)
            normalized += 360;
        return normalized;
    }
}
