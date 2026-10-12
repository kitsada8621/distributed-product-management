namespace Auth.UnitTests.Fakes;

public sealed class FakeTimeProvider : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 10, 10, 8, 30, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => UtcNow;
}
