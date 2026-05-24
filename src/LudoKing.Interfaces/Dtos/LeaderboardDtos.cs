namespace LudoKing.Interfaces.Dtos;

public class LeaderboardEntryDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int EloRating { get; set; }
    public int WinCount { get; set; }
    public int GamesPlayed { get; set; }
    public decimal WinRate { get; set; }
    public int Rank { get; set; }
}
