namespace LudoKing.Interfaces.Entities;

public class FriendRequest
{
    public Guid Id { get; set; }
    public Guid RequesterId { get; set; }
    public Guid TargetId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}
