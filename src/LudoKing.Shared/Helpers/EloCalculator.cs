namespace LudoKing.Shared.Helpers;

public static class EloCalculator
{
    public static (int newWinnerElo, int newLoserElo) Calculate(
        int winnerElo, int loserElo, int kFactor = 32)
    {
        double expectedWinner = 1.0 / (1.0 + Math.Pow(10.0, (loserElo - winnerElo) / 400.0));
        double expectedLoser  = 1.0 - expectedWinner;

        int newWinnerElo = (int)Math.Round(winnerElo + kFactor * (1.0 - expectedWinner));
        int newLoserElo  = (int)Math.Round(loserElo  + kFactor * (0.0 - expectedLoser));

        return (newWinnerElo, newLoserElo);
    }
}
