namespace Infrastructure.Services.Email.Options;

public sealed class SmtpOptions
{
    public string Host        { get; init; } = string.Empty;
    public int    Port        { get; init; } = 587;
    public string Username    { get; init; } = string.Empty;
    public string Password    { get; init; } = string.Empty;
    public bool   UseSsl      { get; init; } = true;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName    { get; init; } = string.Empty;
}
