import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { ProfileComponent } from './profile.component';

const routes: Routes = [
  { path: '', component: ProfileComponent },
];

@NgModule({
  declarations: [ProfileComponent],
  imports: [
    CommonModule, ReactiveFormsModule,
    RouterModule.forChild(routes),
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatTableModule, MatProgressBarModule,
  ],
})
export class ProfileModule {}
