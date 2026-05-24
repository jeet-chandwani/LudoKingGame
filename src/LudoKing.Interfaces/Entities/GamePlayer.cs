namespace LudoKing.Interfaces.Entities;

public class GamePlayer
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public Guid? UserId { get; set; }
    public string ColorAssigned { get; set; } = string.Empty;
    public int SeatPosition { get; set; }
    public int? FinishPosition { get; set; }
    public bool IsConnected { get; set; }
    public bool IsBot { get; set; }
    public int TokensHome { get; set; }
    public int TokensCut { get; set; }
    public int TokensLost { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}
