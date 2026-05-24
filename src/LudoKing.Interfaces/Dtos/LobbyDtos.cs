namespace LudoKing.Interfaces.Dtos;

public class CreateRoomRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public RuleSetDto Rules { get; set; } = new();
}
