namespace LudoKing.Interfaces.Infrastructure;

public interface IEmailService
{
    Task SendEmailConfirmationAsync(string toEmail, string displayName, string confirmationToken);
    Task SendPasswordResetAsync(string toEmail, string displayName, string resetToken);
}
