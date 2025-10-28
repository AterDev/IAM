import { Component, inject, OnInit, ChangeDetectionStrategy, signal } from '@angular/core';
import { FormControl, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatStepperModule } from '@angular/material/stepper';
import { Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { CommonModule } from '@angular/common';
import { ApiClient } from 'src/app/services/api/api-client';

@Component({
  selector: 'app-forgot-password',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatStepperModule,
    TranslateModule
  ],
  templateUrl: './forgot-password.html',
  styleUrls: ['./forgot-password.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ForgotPassword implements OnInit {
  private apiClient = inject(ApiClient);
  private router = inject(Router);
  private translate = inject(TranslateService);

  emailForm!: FormGroup;
  resetForm!: FormGroup;
  
  isLoading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');
  currentStep = signal(0);

  ngOnInit(): void {
    this.emailForm = new FormGroup({
      email: new FormControl('', [Validators.required, Validators.email])
    });

    this.resetForm = new FormGroup({
      code: new FormControl('', [Validators.required, Validators.minLength(6)]),
      newPassword: new FormControl('', [
        Validators.required,
        Validators.minLength(8),
        Validators.maxLength(60)
      ]),
      confirmPassword: new FormControl('', [Validators.required])
    });
  }

  get email() {
    return this.emailForm.get('email') as FormControl;
  }

  get code() {
    return this.resetForm.get('code') as FormControl;
  }

  get newPassword() {
    return this.resetForm.get('newPassword') as FormControl;
  }

  get confirmPassword() {
    return this.resetForm.get('confirmPassword') as FormControl;
  }

  async sendResetCode(): Promise<void> {
    if (this.emailForm.invalid) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    // TODO: Implement forgot password API
    // For now, simulate success
    setTimeout(() => {
      this.isLoading.set(false);
      this.successMessage.set(this.translate.instant('forgotPassword.codeSent'));
      this.currentStep.set(1);
    }, 1000);
  }

  async resetPassword(): Promise<void> {
    if (this.resetForm.invalid) {
      return;
    }

    const { newPassword, confirmPassword } = this.resetForm.value;
    if (newPassword !== confirmPassword) {
      this.errorMessage.set(this.translate.instant('validation.passwordmismatch'));
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    // TODO: Implement reset password API
    // For now, simulate success
    setTimeout(() => {
      this.isLoading.set(false);
      this.successMessage.set(this.translate.instant('forgotPassword.resetSuccess'));
      setTimeout(() => {
        this.router.navigate(['/login']);
      }, 2000);
    }, 1000);
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }
}
