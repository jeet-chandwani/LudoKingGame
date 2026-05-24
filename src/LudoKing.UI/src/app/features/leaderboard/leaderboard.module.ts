import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { LeaderboardComponent } from './leaderboard.component';

const routes: Routes = [{ path: '', component: LeaderboardComponent }];

@NgModule({
  declarations: [LeaderboardComponent],
  imports: [CommonModule, RouterModule.forChild(routes), MatTableModule, MatProgressBarModule],
})
export class LeaderboardModule {}
