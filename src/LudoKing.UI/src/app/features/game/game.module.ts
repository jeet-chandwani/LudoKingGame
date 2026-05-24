import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';

import { BoardComponent } from './board/board.component';
import { DiceComponent } from './dice/dice.component';
import { GamePageComponent } from './game-page/game-page.component';

const routes: Routes = [
  { path: ':id', component: GamePageComponent },
];

@NgModule({
  declarations: [BoardComponent, DiceComponent, GamePageComponent],
  imports: [
    CommonModule,
    RouterModule.forChild(routes),
    MatCardModule, MatButtonModule, MatIconModule, MatChipsModule,
  ],
})
export class GameModule {}
