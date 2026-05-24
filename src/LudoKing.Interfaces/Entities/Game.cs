namespace LudoKing.Interfaces.Entities;

public class Game
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid RuleConfigurationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? WinnerUserId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? DurationSeconds { get; set; }
    public int TotalMoves { get; set; }
}
