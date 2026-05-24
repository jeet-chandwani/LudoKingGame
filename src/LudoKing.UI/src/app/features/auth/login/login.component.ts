import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: false,
  templateUrl: './login.component.html',
})
export class LoginComponent {
  form: FormGroup;
  error = '';
  loading = false;
  hidePassword = true;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
  ) {
    this.form = this.fb.group({
      identifier: ['', Validators.required],
      password:   ['', Validators.required],
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = '';
    const { identifier, password } = this.form.value;
    this.auth.login({ identifier: identifier!, password: password! }).subscribe({
      next: () => { this.loading = false; this.router.navigate(['/lobby']); },
      error: (e) => { this.error = e.error?.message ?? 'Login failed.'; this.loading = false; },
    });
  }
}
