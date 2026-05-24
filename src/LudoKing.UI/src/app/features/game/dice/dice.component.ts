import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-dice',
  standalone: false,
  template: `
    <div class="dice-container">
      <button mat-raised-button color="accent" (click)="roll.emit()"
              [disabled]="!canRoll" class="roll-button">
        Roll Dice
      </button>
      <div class="dice-values" *ngIf="values.length">
        <span class="die" *ngFor="let v of values">{{ dieFace(v) }}</span>
      </div>
    </div>`,
  styles: [`
    .dice-container { display:flex; flex-direction:column; align-items:center; gap:12px; }
    .roll-button    { font-size:16px; padding:12px 24px; height:auto; }
    .dice-values    { display:flex; gap:12px; }
    .die            { font-size:48px; line-height:1; }
  `],
})
export class DiceComponent {
  @Input() values: number[] = [];
  @Input() canRoll = false;
  @Output() roll = new EventEmitter<void>();

  dieFace(v: number): string {
    return ['', '⚀', '⚁', '⚂', '⚃', '⚄', '⚅'][v] ?? '?';
  }
}
