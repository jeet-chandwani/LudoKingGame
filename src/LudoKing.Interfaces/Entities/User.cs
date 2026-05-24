namespace LudoKing.Interfaces.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? CountryCode { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsActive { get; set; }
    public bool IsBanned { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
