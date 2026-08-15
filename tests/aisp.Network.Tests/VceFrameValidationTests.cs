namespace aisp.Network.Tests;

public class VceFrameValidationTests
{
    [Theory]
    [InlineData(1, 4096, true)]
    [InlineData(4096, 4096, true)]
    [InlineData(1392, 4096, true)]
    [InlineData(0, 4096, false)]
    [InlineData(-1, 4096, false)]
    [InlineData(4097, 4096, false)]
    [InlineData(int.MaxValue, 4096, false)]
    public void IsAcceptableFrameSize_ValidatesBounds(
        int msgSize,
        int maxReceiveFrameSize,
        bool expected
    )
    {
        Assert.Equal(
            expected,
            VceFrameValidation.IsAcceptableFrameSize(msgSize, maxReceiveFrameSize)
        );
    }
}
