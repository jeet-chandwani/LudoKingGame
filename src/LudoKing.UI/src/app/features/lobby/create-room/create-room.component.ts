import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

@Component({
  selector: 'app-create-room',
  standalone: false,
  templateUrl: './create-room.component.html',
})
export class CreateRoomComponent {
  form: FormGroup;
  loading = false;
  error = '';

  constructor(private fb: FormBuilder, private http: HttpClient, private router: Router) {
    this.form = this.fb.group({
      name:                ['', [Validators.required, Validators.maxLength(100)]],
      isPrivate:           [false],
      maxPlayers:          [4, [Validators.min(2), Validators.max(4)]],
      tokensPerPlayer:     [4, [Validators.min(1), Validators.max(4)]],
      requireSixToStart:   [true],
      extraTurnOnSix:      [true],
      extraTurnOnCut:      [false],
      maxConsecutiveSixes: [3, [Validators.min(1), Validators.max(5)]],
      homeEntryRule:       ['Exact'],
      stackProtection:     [true],
      safeSquaresMode:     ['Standard'],
      turnTimerSeconds:    [30, [Validators.min(0), Validators.max(300)]],
      stopOnFirstWinner:   [false],
      automatedPlayers:    [0, [Validators.min(0), Validators.max(3)]],
      botRequireSixToStart:[false],
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    const v = this.form.value;
    this.loading = true;
    this.http.post<{ id: string }>('/api/v1/rooms', {
      name: v.name,
      isPrivate: v.isPrivate,
      rules: {
        maxPlayers: v.maxPlayers,
        tokensPerPlayer: v.tokensPerPlayer,
        requireSixToStart: v.requireSixToStart,
        extraTurnOnSix: v.extraTurnOnSix,
        extraTurnOnCut: v.extraTurnOnCut,
        maxConsecutiveSixes: v.maxConsecutiveSixes,
        homeEntryRule: v.homeEntryRule,
        stackProtectionEnabled: v.stackProtection,
        safeSquaresMode: v.safeSquaresMode,
        turnTimerSeconds: v.turnTimerSeconds,
        stopOnFirstWinner: v.stopOnFirstWinner,
        automatedPlayerCount: v.automatedPlayers,
        botRequireSixToStart: v.botRequireSixToStart,
      }
    }).subscribe({
      next: res => this.router.navigate(['/lobby/room', res.id]),
      error: e => { this.error = e.error?.message ?? 'Failed to create room.'; this.loading = false; },
    });
  }
}
