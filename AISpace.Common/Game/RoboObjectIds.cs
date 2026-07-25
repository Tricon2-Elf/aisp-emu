namespace AISpace.Common.Game;

public static class RoboObjectIds
{
    public const uint Base = 2_000_000_000u;

    public static uint For(uint roboId) => Base + roboId;
}
