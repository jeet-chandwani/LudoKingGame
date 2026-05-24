export interface RuleSet {
  maxPlayers: number;
  tokensPerPlayer: number;
  diceCount: number;
  requireSixToStart: boolean;
  extraTurnOnSix: boolean;
  extraTurnOnCut: boolean;
  maxConsecutiveSixes: number;
  homeEntryRule: 'Exact' | 'BounceBack';
  stackProtectionEnabled: boolean;
  safeSquaresMode: 'Standard' | 'Extended' | 'None';
  turnTimerSeconds: number;
  autoKickOnDisconnectSeconds: number;
  stopOnFirstWinner: boolean;
  allowSpectators: boolean;
  automatedPlayerCount: number;
  botRequireSixToStart: boolean;
}

export interface TokenState {
  index: number;
  square: number;
  isHome: boolean;
}

export interface PlayerState {
  userId: string;
  color: string;
  seatPosition: number;
  isConnected: boolean;
  isBot: boolean;
  hasFinished: boolean;
  finishPosition: number;
  tokens: TokenState[];
}

export interface ValidMove {
  tokenIndex: number;
  targetSquare: number;
  wouldCut: boolean;
}

export interface GameState {
  gameId: string;
  status: string;
  currentTurnUserId: string;
  turnNumber: number;
  consecutiveSixCount: number;
  waitingForMove: boolean;
  lastDiceValue: number | null;
  lastValidMoves: ValidMove[];
  rules: RuleSet;
  players: PlayerState[];
}

// SignalR event payloads
export interface DiceRolledEvent {
  gameId: string;
  rollingUserId: string;
  diceValues: number[];
  hasValidMoves: boolean;
}

export interface ValidMovesForYouEvent {
  gameId: string;
  moves: ValidMove[];
}

export interface TokenMovedEvent {
  gameId: string;
  userId: string;
  tokenIndex: number;
  fromSquare: number;
  toSquare: number;
}

export interface TokenCutEvent {
  gameId: string;
  attackerUserId: string;
  victimUserId: string;
  victimTokenIndex: number;
}

export interface PlayerFinishedEvent {
  gameId: string;
  userId: string;
  finishPosition: number;
}

export interface TurnChangedEvent {
  gameId: string;
  nextTurnUserId: string;
  turnNumber: number;
  timerEndsAt: string | null;
}

export interface GameOverEvent {
  gameId: string;
  finalRankings: { position: number; userId: string; displayName: string }[];
}
