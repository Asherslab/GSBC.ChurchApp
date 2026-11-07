namespace GSBC.ChurchApp.Testing;

public sealed class TestClock(DateTime utcNow) : TimeProvider
{
    public static readonly DateTime Start = new(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc);

    public TestClock() : this(Start)
    {
    }

    public DateTime UtcNow { get; set; } = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);

    public override DateTimeOffset GetUtcNow() => new(UtcNow, TimeSpan.Zero);

    public void Advance(TimeSpan by) => UtcNow += by;
}
