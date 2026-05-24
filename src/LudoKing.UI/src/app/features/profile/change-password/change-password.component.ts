import { Component } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const newPw = group.get('newPassword')?.value;
  const confirm = group.get('confirmPassword')?.value;
  return newPw && confirm && newPw !== confirm ? { mismatch: true } : null;
}

@Component({
  selector: 'app-change-password',
  standalone: false,
  templateUrl: './change-password.component.html',
})
export class ChangePasswordComponent {
  form: FormGroup;
  loading = false;
  success = false;
  error = '';
  hideCurrentPw = true;
  hideNewPw = true;
  hideConfirmPw = true;

  constructor(private fb: FormBuilder, private auth: AuthService, private router: Router) {
    this.form = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required],
    }, { validators: passwordsMatch });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = '';
    const { currentPassword, newPassword } = this.form.value;
    this.auth.changePassword({ currentPassword, newPassword }).subscribe({
      next: () => {
        this.success = true;
        this.loading = false;
        this.auth.clearAuth();
        setTimeout(() => this.router.navigate(['/auth/login']), 2000);
      },
      error: e => {
        this.error = e.error?.message ?? e.error?.error ?? 'Password change failed.';
        this.loading = false;
      },
    });
  }
}
