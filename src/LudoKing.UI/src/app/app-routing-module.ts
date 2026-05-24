import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

const routes: Routes = [
  { path: 'auth',        loadChildren: () => import('./features/auth/auth.module').then(m => m.AuthModule) },
  { path: 'lobby',       loadChildren: () => import('./features/lobby/lobby.module').then(m => m.LobbyModule),      canActivate: [authGuard] },
  { path: 'game',        loadChildren: () => import('./features/game/game.module').then(m => m.GameModule),          canActivate: [authGuard] },
  { path: 'profile',     loadChildren: () => import('./features/profile/profile.module').then(m => m.ProfileModule), canActivate: [authGuard] },
  { path: 'leaderboard', loadChildren: () => import('./features/leaderboard/leaderboard.module').then(m => m.LeaderboardModule) },
  { path: '', redirectTo: 'lobby', pathMatch: 'full' },
  { path: '**', redirectTo: 'lobby' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
