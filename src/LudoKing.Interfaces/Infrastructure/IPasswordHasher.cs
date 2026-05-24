namespace LudoKing.Interfaces.Infrastructure;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string hash, string password);
}
