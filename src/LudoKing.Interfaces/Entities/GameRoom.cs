namespace LudoKing.Interfaces.Entities;

public class GameRoom
{
    public Guid Id { get; set; }
    public Guid HostUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string JoinCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public int MaxPlayers { get; set; }
    public int CurrentPlayerCount { get; set; }
    public Guid RuleConfigurationId { get; set; }
    public Guid? ActiveGameId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}
