using LudoKing.Interfaces.Infrastructure;
using Microsoft.Extensions.Logging;

namespace LudoKing.Infrastructure.Email;

public sealed class StubEmailService : IEmailService
{
    private readonly ILogger<StubEmailService> _logger;

    public StubEmailService(ILogger<StubEmailService> logger) => _logger = logger;

    public Task SendEmailConfirmationAsync(string toEmail, string displayName, string confirmationToken)
    {
        _logger.LogInformation(
            "[STUB EMAIL] Confirmation email to {Email} ({DisplayName}). Token: {Token}",
            toEmail, displayName, confirmationToken);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string toEmail, string displayName, string resetToken)
    {
        _logger.LogInformation(
            "[STUB EMAIL] Password reset email to {Email} ({DisplayName}). Token: {Token}",
            toEmail, displayName, resetToken);
        return Task.CompletedTask;
    }
}
