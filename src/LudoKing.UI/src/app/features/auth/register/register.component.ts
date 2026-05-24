import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-register',
  standalone: false,
  templateUrl: './register.component.html',
})
export class RegisterComponent {
  form: FormGroup;
  error = '';
  success = '';
  loading = false;
  hidePassword = true;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
  ) {
    this.form = this.fb.group({
      displayName: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50)]],
      email:       ['', [Validators.required, Validators.email]],
      password:    ['', [Validators.required, Validators.minLength(8)]],
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = '';
    const { displayName, email, password } = this.form.value;
    this.auth.register({ displayName: displayName!, email: email!, password: password! }).subscribe({
      next: () => {
        this.success = 'Account created! Check your email to confirm before logging in.';
        this.loading = false;
      },
      error: (e) => { this.error = e.error?.message ?? 'Registration failed.'; this.loading = false; },
    });
  }
}
