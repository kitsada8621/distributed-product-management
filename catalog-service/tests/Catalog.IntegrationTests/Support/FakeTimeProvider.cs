namespace Catalog.IntegrationTests.Support;

public sealed class FakeTimeProvider : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 10, 10, 8, 30, 0, TimeSpan.Zero);

    public void Advance(TimeSpan by)
    {
        UtcNow += by;
    }

    public override DateTimeOffset GetUtcNow() => UtcNow;
}
