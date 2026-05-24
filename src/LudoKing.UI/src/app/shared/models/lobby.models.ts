import { RuleSet } from './game.models';

export interface CreateRoomRequest {
  name: string;
  isPrivate: boolean;
  rules: Partial<RuleSet>;
}

export interface RoomSummary {
  id: string;
  name: string;
  hostDisplayName: string;
  currentPlayers: number;
  maxPlayers: number;
  isPrivate: boolean;
  status: string;
}

export interface RoomDetail {
  id: string;
  name: string;
  hostUserId: string;
  joinCode: string | null;
  status: string;
  isPrivate: boolean;
  players: RoomPlayer[];
  rules: RuleSet;
}

export interface RoomPlayer {
  userId: string;
  displayName: string;
  avatarUrl: string | null;
  seatPosition: number;
  isReady: boolean;
  isHost: boolean;
}
