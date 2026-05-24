import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { UserStats, MatchHistoryEntry } from '../../shared/models/leaderboard.models';

interface UserProfile {
  id: string; displayName: string; email: string;
  avatarUrl: string | null; countryCode: string | null; bio: string | null;
  createdAt: string;
}

@Component({
  selector: 'app-profile',
  standalone: false,
  templateUrl: './profile.component.html',
})
export class ProfileComponent implements OnInit {
  profile: UserProfile | null = null;
  stats: UserStats | null = null;
  history: MatchHistoryEntry[] = [];
  editing = false;
  loading = true;
  saving = false;
  error = '';
  form: FormGroup;

  constructor(private http: HttpClient, private fb: FormBuilder) {
    this.form = this.fb.group({
      displayName: ['', [Validators.required, Validators.minLength(3)]],
      bio: ['', Validators.maxLength(300)],
      countryCode: [''],
    });
  }

  ngOnInit(): void {
    this.http.get<UserProfile>('/api/v1/users/me').subscribe(p => {
      this.profile = p;
      this.form.patchValue({ displayName: p.displayName, bio: p.bio ?? '', countryCode: p.countryCode ?? '' });
      this.loading = false;
    });
    this.http.get<UserStats>('/api/v1/users/me/stats').subscribe(s => this.stats = s);
    this.http.get<MatchHistoryEntry[]>('/api/v1/users/me/history').subscribe(h => this.history = h);
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving = true;
    this.http.put('/api/v1/users/me', this.form.value).subscribe({
      next: () => { this.editing = false; this.saving = false; },
      error: e => { this.error = e.error?.message ?? 'Save failed.'; this.saving = false; },
    });
  }
}
