namespace LudoKing.Interfaces.Entities;

public class GameMove
{
    public long Id { get; set; }
    public Guid GameId { get; set; }
    public int SequenceNumber { get; set; }
    public Guid UserId { get; set; }
    public string MoveType { get; set; } = string.Empty;
    public int DiceValue { get; set; }
    public int? TokenIndex { get; set; }
    public int? FromSquare { get; set; }
    public int? ToSquare { get; set; }
    public bool WasCut { get; set; }
    public Guid? CutTokenOwnerId { get; set; }
    public string? BoardStateSnapshot { get; set; }
    public DateTime OccurredAt { get; set; }
}
