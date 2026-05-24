import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RoomSummary } from '../../../shared/models/lobby.models';

@Component({
  selector: 'app-room-list',
  standalone: false,
  templateUrl: './room-list.component.html',
})
export class RoomListComponent implements OnInit {
  rooms: RoomSummary[] = [];
  loading = true;
  displayedColumns = ['name', 'host', 'players', 'status', 'join'];

  constructor(private http: HttpClient) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.http.get<RoomSummary[]>('/api/v1/rooms').subscribe({
      next: rooms => { this.rooms = rooms; this.loading = false; },
      error: ()   => { this.loading = false; },
    });
  }

  quickMatch(): void {
    this.http.get<{ roomId: string }>('/api/v1/rooms/quick-match').subscribe({
      next: res => window.location.href = `/lobby/room/${res.roomId}`,
    });
  }
}
