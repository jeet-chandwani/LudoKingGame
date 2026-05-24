import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { LeaderboardEntry } from '../../shared/models/leaderboard.models';

@Component({
  selector: 'app-leaderboard',
  standalone: false,
  templateUrl: './leaderboard.component.html',
})
export class LeaderboardComponent implements OnInit {
  entries: LeaderboardEntry[] = [];
  loading = true;
  displayedColumns = ['rank', 'player', 'elo', 'wins', 'games', 'winRate'];

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.http.get<{ entries: LeaderboardEntry[] }>('/api/v1/leaderboard').subscribe({
      next: res => { this.entries = res.entries; this.loading = false; },
      error: ()  => { this.loading = false; },
    });
  }
}
