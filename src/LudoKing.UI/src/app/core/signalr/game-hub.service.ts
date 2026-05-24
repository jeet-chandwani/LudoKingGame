import { Injectable, OnDestroy } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import {
  GameState, DiceRolledEvent, ValidMovesForYouEvent,
  TokenMovedEvent, TokenCutEvent, PlayerFinishedEvent,
  TurnChangedEvent, GameOverEvent
} from '../../shared/models/game.models';

@Injectable({ providedIn: 'root' })
export class GameHubService implements OnDestroy {
  private _hub: HubConnection | null = null;

  readonly gameStateSnapshot$ = new Subject<GameState>();
  readonly diceRolled$        = new Subject<DiceRolledEvent>();
  readonly validMovesForYou$  = new Subject<ValidMovesForYouEvent>();
  readonly tokenMoved$        = new Subject<TokenMovedEvent>();
  readonly tokenCut$          = new Subject<TokenCutEvent>();
  readonly playerFinished$    = new Subject<PlayerFinishedEvent>();
  readonly turnChanged$       = new Subject<TurnChangedEvent>();
  readonly turnTimedOut$      = new Subject<{ gameId: string; userId: string }>();
  readonly playerConnected$   = new Subject<{ gameId: string; userId: string }>();
  readonly playerDisconnected$ = new Subject<{ gameId: string; userId: string; autoKickInSeconds: number }>();
  readonly gameOver$          = new Subject<GameOverEvent>();
  readonly gameAbandoned$     = new Subject<{ gameId: string; reason: string }>();

  constructor(private auth: AuthService) {}

  async connect(): Promise<void> {
    if (this._hub) return;
    this._hub = new HubConnectionBuilder()
      .withUrl('/hubs/game', { accessTokenFactory: () => this.auth.accessToken ?? '' })
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

  async joinGame(gameId: string): Promise<void> {
    await this._hub?.invoke('JoinGame', { gameId });
  }

  async leaveGame(gameId: string): Promise<void> {
    await this._hub?.invoke('LeaveGame', { gameId });
  }

  async rollDice(gameId: string): Promise<void> {
    await this._hub?.invoke('RollDice', { gameId });
  }

  async moveToken(gameId: string, tokenIndex: number, targetSquare: number): Promise<void> {
    await this._hub?.invoke('MoveToken', { gameId, tokenIndex, targetSquare });
  }

  private registerHandlers(): void {
    if (!this._hub) return;
    this._hub.on('GameStateSnapshot',  (d: GameState)              => this.gameStateSnapshot$.next(d));
    this._hub.on('DiceRolled',         (d: DiceRolledEvent)         => this.diceRolled$.next(d));
    this._hub.on('ValidMovesForYou',   (d: ValidMovesForYouEvent)   => this.validMovesForYou$.next(d));
    this._hub.on('TokenMoved',         (d: TokenMovedEvent)         => this.tokenMoved$.next(d));
    this._hub.on('TokenCut',           (d: TokenCutEvent)           => this.tokenCut$.next(d));
    this._hub.on('PlayerFinished',     (d: PlayerFinishedEvent)     => this.playerFinished$.next(d));
    this._hub.on('TurnChanged',        (d: TurnChangedEvent)        => this.turnChanged$.next(d));
    this._hub.on('TurnTimedOut',       (d: any)                     => this.turnTimedOut$.next(d));
    this._hub.on('PlayerConnected',    (d: any)                     => this.playerConnected$.next(d));
    this._hub.on('PlayerDisconnected', (d: any)                     => this.playerDisconnected$.next(d));
    this._hub.on('GameOver',           (d: GameOverEvent)           => this.gameOver$.next(d));
    this._hub.on('GameAbandoned',      (d: any)                     => this.gameAbandoned$.next(d));
  }

  ngOnDestroy(): void { this.disconnect(); }
}
