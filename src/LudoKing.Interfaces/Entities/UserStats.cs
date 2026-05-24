namespace LudoKing.Interfaces.Entities;

public class UserStats
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int GamesPlayed { get; set; }
    public int GamesWon { get; set; }
    public int GamesLost { get; set; }
    public int GamesAbandoned { get; set; }
    public int TotalTokensCut { get; set; }
    public int TotalTokensLost { get; set; }
    public long TotalPlayTimeSeconds { get; set; }
    public int EloRating { get; set; }
    public int CurrentWinStreak { get; set; }
    public int BestWinStreak { get; set; }
    public DateTime UpdatedAt { get; set; }
}
