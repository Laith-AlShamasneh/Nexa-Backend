namespace Application.Interfaces.Services;

public interface ITokenHasher
{
    string GenerateRawToken();
    string Hash(string rawToken);
}
