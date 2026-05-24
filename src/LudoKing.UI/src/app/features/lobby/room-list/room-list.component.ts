import { ChangeDetectorRef, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { finalize } from 'rxjs';
import { RoomSummary } from '../../../shared/models/lobby.models';

@Component({
  selector: 'app-room-list',
  standalone: false,
  templateUrl: './room-list.component.html',
})
export class RoomListComponent implements OnInit {
  rooms: RoomSummary[] = [];
  loading = false;
  displayedColumns = ['name', 'host', 'players', 'status', 'join'];

  private readonly destroyRef = inject(DestroyRef);
  private readonly cdr = inject(ChangeDetectorRef);

  constructor(private http: HttpClient) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.http.get<RoomSummary[]>('/api/v1/rooms')
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => { this.loading = false; this.cdr.detectChanges(); })
      )
      .subscribe({
        next: rooms => { this.rooms = rooms; },
        error: () => {},
      });
  }

  quickMatch(): void {
    this.http.get<{ roomId: string }>('/api/v1/rooms/quick-match').subscribe({
      next: res => window.location.href = `/lobby/room/${res.roomId}`,
    });
  }
}
