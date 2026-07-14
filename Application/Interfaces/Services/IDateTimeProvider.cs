namespace Application.Interfaces.Services;

/// <summary>
/// Testable UTC clock. Application/Infrastructure code that needs "now" for a
/// security- or audit-relevant timestamp should take this instead of calling
/// <c>DateTime.UtcNow</c> directly, so it can be substituted in tests.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
