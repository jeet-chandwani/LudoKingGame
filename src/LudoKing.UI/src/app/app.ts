import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './core/auth/auth.service';

@Component({
  selector: 'app-root',
  standalone: false,
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  constructor(public auth: AuthService, private router: Router) {}

  logout(): void {
    this.auth.logout().subscribe({ error: () => {} });
    this.router.navigate(['/auth/login']);
  }
}
