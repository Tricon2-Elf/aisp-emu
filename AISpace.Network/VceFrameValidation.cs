namespace AISpace.Network;

public static class VceFrameValidation
{
    public static bool IsAcceptableFrameSize(int msgSize, int maxReceiveFrameSize) => msgSize > 0 && msgSize <= maxReceiveFrameSize;
}
