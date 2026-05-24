import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { GameHubService } from '../../../core/signalr/game-hub.service';
import { AuthService } from '../../../core/auth/auth.service';
import {
  GameState, ValidMove, DiceRolledEvent, TokenMovedEvent,
  TokenCutEvent, TurnChangedEvent, GameOverEvent
} from '../../../shared/models/game.models';
import { COLOR_HEX } from '../../../shared/constants/board-constants';

@Component({
  selector: 'app-game-page',
  standalone: false,
  templateUrl: './game-page.component.html',
})
export class GamePageComponent implements OnInit, OnDestroy {
  state: GameState | null = null;
  validMoves: ValidMove[] = [];
  diceValues: number[] = [];
  statusMessages: string[] = [];
  gameOver: GameOverEvent | null = null;
  private subs: Subscription[] = [];

  get gameId(): string { return this.route.snapshot.paramMap.get('id')!; }
  get myUserId(): string { return this.auth.currentUser?.userId ?? ''; }
  get isMyTurn(): boolean { return this.state?.currentTurnUserId === this.myUserId; }
  get canRoll(): boolean { return this.isMyTurn && !this.state?.waitingForMove; }
  get colorHex() { return COLOR_HEX; }

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    public hub: GameHubService,
    private auth: AuthService,
  ) {}

  async ngOnInit(): Promise<void> {
    await this.hub.connect();

    this.subs.push(
      this.hub.gameStateSnapshot$.subscribe(s => { this.state = s; this.validMoves = []; this.diceValues = []; }),
      this.hub.diceRolled$.subscribe(e => this.onDiceRolled(e)),
      this.hub.validMovesForYou$.subscribe(e => { this.validMoves = e.moves; }),
      this.hub.tokenMoved$.subscribe(e => this.onTokenMoved(e)),
      this.hub.tokenCut$.subscribe(e => this.onTokenCut(e)),
      this.hub.turnChanged$.subscribe(e => this.onTurnChanged(e)),
      this.hub.gameOver$.subscribe(e => { this.gameOver = e; }),
      this.hub.gameAbandoned$.subscribe(() => { this.pushMsg('Game abandoned.'); setTimeout(() => this.router.navigate(['/lobby']), 3000); }),
    );

    await this.hub.joinGame(this.gameId);
  }

  async ngOnDestroy(): Promise<void> {
    this.subs.forEach(s => s.unsubscribe());
    await this.hub.leaveGame(this.gameId);
    await this.hub.disconnect();
  }

  async rollDice(): Promise<void> {
    await this.hub.rollDice(this.gameId);
  }

  async selectMove(move: ValidMove): Promise<void> {
    await this.hub.moveToken(this.gameId, move.tokenIndex, move.targetSquare);
    this.validMoves = [];
  }

  private onDiceRolled(e: DiceRolledEvent): void {
    this.diceValues = e.diceValues;
    const name = this.playerName(e.rollingUserId);
    this.pushMsg(`${name} rolled ${e.diceValues.join(', ')}`);
    if (!e.hasValidMoves) this.pushMsg(`${name} has no valid moves — turn skipped.`);
  }

  private onTokenMoved(e: TokenMovedEvent): void {
    if (!this.state) return;
    const player = this.state.players.find(p => p.userId === e.userId);
    if (player) {
      const token = player.tokens.find(t => t.index === e.tokenIndex);
      if (token) { token.square = e.toSquare; }
    }
    this.state = { ...this.state };
  }

  private onTokenCut(e: TokenCutEvent): void {
    if (!this.state) return;
    const victim = this.state.players.find(p => p.userId === e.victimUserId);
    if (victim) {
      const t = victim.tokens.find(t => t.index === e.victimTokenIndex);
      if (t) t.square = -1;
    }
    this.state = { ...this.state };
    const atk = this.playerName(e.attackerUserId);
    const vic = this.playerName(e.victimUserId);
    this.pushMsg(`${atk} cut ${vic}'s token!`);
  }

  private onTurnChanged(e: TurnChangedEvent): void {
    if (!this.state) return;
    this.state = { ...this.state, currentTurnUserId: e.nextTurnUserId, waitingForMove: false };
    this.validMoves = [];
    this.diceValues = [];
    this.pushMsg(`Turn: ${this.playerName(e.nextTurnUserId)}`);
  }

  private playerName(userId: string): string {
    const p = this.state?.players.find(p => p.userId === userId);
    return p ? (p.isBot ? `Bot (${p.color})` : userId) : userId;
  }

  private pushMsg(msg: string): void {
    this.statusMessages = [msg, ...this.statusMessages.slice(0, 9)];
  }
}
