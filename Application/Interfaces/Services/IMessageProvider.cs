namespace Application.Interfaces.Services;

public interface IMessageProvider
{
    Task<string> GetMessagesAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, string>> GetLabelsAsync(string section, CancellationToken ct = default);
}
