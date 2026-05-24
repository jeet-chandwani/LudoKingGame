import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatListModule } from '@angular/material/list';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';

import { RoomListComponent } from './room-list/room-list.component';
import { CreateRoomComponent } from './create-room/create-room.component';
import { RoomWaitingComponent } from './room-waiting/room-waiting.component';

const routes: Routes = [
  { path: '',          component: RoomListComponent },
  { path: 'create',   component: CreateRoomComponent },
  { path: 'room/:id', component: RoomWaitingComponent },
];

@NgModule({
  declarations: [RoomListComponent, CreateRoomComponent, RoomWaitingComponent],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule.forChild(routes),
    MatCardModule, MatTableModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatCheckboxModule, MatChipsModule, MatListModule,
    MatProgressBarModule, MatTooltipModule,
  ],
})
export class LobbyModule {}
