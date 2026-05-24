export interface LeaderboardEntry {
  rank: number;
  userId: string;
  displayName: string;
  avatarUrl: string | null;
  eloRating: number;
  gamesPlayed: number;
  gamesWon: number;
  winRate: number;
}

export interface UserStats {
  userId: string;
  displayName: string;
  gamesPlayed: number;
  gamesWon: number;
  gamesLost: number;
  gamesAbandoned: number;
  totalTokensCut: number;
  totalTokensLost: number;
  eloRating: number;
  currentWinStreak: number;
  bestWinStreak: number;
}

export interface MatchHistoryEntry {
  gameId: string;
  startedAt: string;
  endedAt: string | null;
  finishPosition: number | null;
  totalPlayers: number;
}
