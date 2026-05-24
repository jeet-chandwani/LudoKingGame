using LudoKing.Interfaces.Dtos;

namespace LudoKing.Interfaces.Services;

public interface IGameEngine
{
    GameStateDto InitializeGame(Guid gameId, List<PlayerStateDto> players, RuleSetDto rules);
    int RollDice(int diceCount);
    int[] RollDiceAll(int diceCount);
    List<ValidMoveDto> ComputeValidMoves(GameStateDto state, string playerId, int diceValue);
    MoveResultDto ApplyMove(GameStateDto state, string playerId, int tokenIndex, int targetSquare, int diceValue);
    bool CheckWin(GameStateDto state, string playerId);
    GameStateDto AdvanceTurn(GameStateDto state, bool grantExtraTurn);
}
