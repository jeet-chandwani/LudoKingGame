import { Injectable, OnDestroy } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { AuthService } from '../auth/auth.service';

export interface LobbyPlayerJoined { roomId: string; userId: string; displayName: string; seatPosition: number; }
export interface LobbyPlayerLeft   { roomId: string; userId: string; }
export interface LobbyReadyChanged  { roomId: string; userId: string; isReady: boolean; }
export interface LobbyGameStarting  { roomId: string; gameId: string; countdownSeconds: number; }

@Injectable({ providedIn: 'root' })
export class LobbyHubService implements OnDestroy {
  private _hub: HubConnection | null = null;

  readonly playerJoined$ = new Subject<LobbyPlayerJoined>();
  readonly playerLeft$   = new Subject<LobbyPlayerLeft>();
  readonly readyChanged$ = new Subject<LobbyReadyChanged>();
  readonly gameStarting$ = new Subject<LobbyGameStarting>();
  readonly roomUpdated$  = new Subject<{ roomId: string }>();
  readonly hostChanged$  = new Subject<{ roomId: string; newHostUserId: string }>();
  readonly playerKicked$ = new Subject<{ roomId: string; kickedUserId: string }>();
  readonly roomClosed$   = new Subject<{ roomId: string; reason: string }>();

  constructor(private auth: AuthService) {}

  async connect(): Promise<void> {
    if (this._hub) return;
    this._hub = new HubConnectionBuilder()
      .withUrl('/hubs/lobby', { accessTokenFactory: () => this.auth.accessToken ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.registerHandlers();
    await this._hub.start();
  }

  async disconnect(): Promise<void> {
    await this._hub?.stop();
    this._hub = null;
  }

  async joinRoomLobby(roomId: string): Promise<void> {
    await this._hub?.invoke('JoinRoomLobby', { roomId });
  }

  async leaveRoomLobby(roomId: string): Promise<void> {
    await this._hub?.invoke('LeaveRoomLobby', { roomId });
  }

  async setReady(roomId: string, isReady: boolean): Promise<void> {
    await this._hub?.invoke('SetReady', { roomId, isReady });
  }

  async requestStart(roomId: string): Promise<void> {
    await this._hub?.invoke('RequestStart', { roomId });
  }

  private registerHandlers(): void {
    if (!this._hub) return;
    this._hub.on('PlayerJoinedLobby', (d: LobbyPlayerJoined) => this.playerJoined$.next(d));
    this._hub.on('PlayerLeftLobby',   (d: LobbyPlayerLeft)   => this.playerLeft$.next(d));
    this._hub.on('PlayerReadyChanged',(d: LobbyReadyChanged)  => this.readyChanged$.next(d));
    this._hub.on('GameStarting',      (d: LobbyGameStarting)  => this.gameStarting$.next(d));
    this._hub.on('RoomUpdated',       (d: { roomId: string }) => this.roomUpdated$.next(d));
    this._hub.on('HostChanged',       (d: any) => this.hostChanged$.next(d));
    this._hub.on('PlayerKicked',      (d: any) => this.playerKicked$.next(d));
    this._hub.on('RoomClosed',        (d: any) => this.roomClosed$.next(d));
  }

  ngOnDestroy(): void { this.disconnect(); }
}
