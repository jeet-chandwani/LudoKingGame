// Must stay in sync with LudoKing.Shared/Constants/BoardConstants.cs

export const OUTER_TRACK_LENGTH = 52;
export const HOME_STRETCH_LENGTH = 5;
export const HOME_CENTER_SQUARE = 999;
export const YARD_SQUARE = -1;

export const COLOR_START_SQUARES: Record<string, number> = {
  Red: 0, Blue: 13, Green: 26, Yellow: 39
};

export const COLOR_HOME_ENTRY_SQUARES: Record<string, number> = {
  Red: 50, Blue: 11, Green: 24, Yellow: 37
};

export const HOME_STRETCH_START: Record<string, number> = {
  Red: 100, Blue: 110, Green: 120, Yellow: 130
};

export const SAFE_SQUARES = new Set([0, 8, 13, 21, 26, 34, 39, 47]);

export function isHomeStretchSquare(square: number): boolean {
  return square >= 100 && square <= 134;
}

export function isSafeSquare(square: number): boolean {
  return SAFE_SQUARES.has(square);
}

export const PLAYER_COLORS = ['Red', 'Blue', 'Green', 'Yellow'];

export const COLOR_HEX: Record<string, string> = {
  Red: '#e53935', Blue: '#1e88e5', Green: '#43a047', Yellow: '#fdd835'
};

// 15×15 board pixel helpers (canvas 600×600)
export const BOARD_SIZE = 600;
export const CELL = BOARD_SIZE / 15;  // 40px per cell
