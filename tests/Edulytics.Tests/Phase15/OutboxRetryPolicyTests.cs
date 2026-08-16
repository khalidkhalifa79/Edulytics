using Edulytics.Core.Reliability;

namespace Edulytics.Tests.Phase15;

public sealed class OutboxRetryPolicyTests
{
    [Fact]
    public void Backoff_IsExponentialBoundedAndJittered()
    {
        var first =
            OutboxRetryPolicy
                .ComputeDelay(
                    1,
                    2,
                    120,
                    750,
                    500);

        var fourth =
            OutboxRetryPolicy
                .ComputeDelay(
                    4,
                    2,
                    120,
                    750,
                    500);

        var huge =
            OutboxRetryPolicy
                .ComputeDelay(
                    50,
                    2,
                    120,
                    750,
                    750);

        Assert.Equal(
            TimeSpan.FromMilliseconds(
                2500),
            first);

        Assert.Equal(
            TimeSpan.FromMilliseconds(
                16500),
            fourth);

        Assert.Equal(
            TimeSpan.FromMilliseconds(
                120750),
            huge);
    }

    [Fact]
    public void InvalidJitterOverride_IsRejected()
    {
        Assert.Throws<
            ArgumentOutOfRangeException>(
                () =>
                    OutboxRetryPolicy
                        .ComputeDelay(
                            1,
                            2,
                            120,
                            750,
                            751));
    }
}
