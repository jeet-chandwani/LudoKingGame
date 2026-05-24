import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: false,
  templateUrl: './forgot-password.component.html',
})
export class ForgotPasswordComponent {
  form: FormGroup;
  sent = false;
  loading = false;

  constructor(private fb: FormBuilder, private auth: AuthService) {
    this.form = this.fb.group({ email: ['', [Validators.required, Validators.email]] });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.auth.forgotPassword(this.form.value.email!).subscribe({
      next: () => { this.sent = true; this.loading = false; },
      error: () => { this.sent = true; this.loading = false; },
    });
  }
}
