namespace LudoKing.Interfaces.Infrastructure;

public interface IAuthConfiguration
{
    string TokenSecret { get; }
    int AccessTokenMinutes { get; }
    int RefreshTokenDays { get; }
}
