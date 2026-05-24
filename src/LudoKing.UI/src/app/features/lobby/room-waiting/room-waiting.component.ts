import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { LobbyHubService } from '../../../core/signalr/lobby-hub.service';
import { AuthService } from '../../../core/auth/auth.service';
import { RoomDetail } from '../../../shared/models/lobby.models';

@Component({
  selector: 'app-room-waiting',
  standalone: false,
  templateUrl: './room-waiting.component.html',
})
export class RoomWaitingComponent implements OnInit, OnDestroy {
  room: RoomDetail | null = null;
  loading = true;
  isReady = false;
  private subs: Subscription[] = [];

  get roomId(): string { return this.route.snapshot.paramMap.get('id')!; }
  get myUserId(): string { return this.auth.currentUser?.userId ?? ''; }
  get isHost(): boolean { return this.room?.hostUserId === this.myUserId; }

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient,
    public lobby: LobbyHubService,
    private auth: AuthService,
  ) {}

  async ngOnInit(): Promise<void> {
    this.loadRoom();
    await this.lobby.connect();
    await this.lobby.joinRoomLobby(this.roomId);

    this.subs.push(
      this.lobby.playerJoined$.subscribe(() => this.loadRoom()),
      this.lobby.playerLeft$.subscribe(()   => this.loadRoom()),
      this.lobby.readyChanged$.subscribe(() => this.loadRoom()),
      this.lobby.gameStarting$.subscribe(e  => this.router.navigate(['/game', e.gameId])),
      this.lobby.roomClosed$.subscribe(()   => this.router.navigate(['/lobby'])),
      this.lobby.playerKicked$.subscribe(e  => {
        if (e.kickedUserId === this.myUserId) this.router.navigate(['/lobby']);
        else this.loadRoom();
      }),
    );
  }

  async ngOnDestroy(): Promise<void> {
    this.subs.forEach(s => s.unsubscribe());
    await this.lobby.leaveRoomLobby(this.roomId);
  }

  loadRoom(): void {
    this.http.get<RoomDetail>(`/api/v1/rooms/${this.roomId}`).subscribe({
      next: r => { this.room = r; this.loading = false; },
      error: () => this.router.navigate(['/lobby']),
    });
  }

  async toggleReady(): Promise<void> {
    this.isReady = !this.isReady;
    await this.lobby.setReady(this.roomId, this.isReady);
  }

  async startGame(): Promise<void> {
    await this.lobby.requestStart(this.roomId);
  }

  kick(userId: string): void {
    this.http.post(`/api/v1/rooms/${this.roomId}/kick/${userId}`, {}).subscribe();
  }

  copyJoinCode(): void {
    if (this.room?.joinCode) navigator.clipboard.writeText(this.room.joinCode);
  }
}
