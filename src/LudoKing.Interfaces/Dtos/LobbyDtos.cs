namespace LudoKing.Interfaces.Dtos;

public class CreateRoomRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public RuleSetDto Rules { get; set; } = new();
}

public class RoomSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string HostDisplayName { get; set; } = string.Empty;
    public int CurrentPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public bool IsPrivate { get; set; }
    public string Status { get; set; } = string.Empty;
}
