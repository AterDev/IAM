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
import { firstValueFrom } from 'rxjs';
import { AccountService } from 'src/app/services/account.service';
import { I18N_KEYS } from 'src/app/share/i18n-keys';

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
  readonly i18n = I18N_KEYS;
  private accountService = inject(AccountService);
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

    try {
      await firstValueFrom(this.accountService.requestPasswordReset({ email: this.email.value }));
      this.isLoading.set(false);
      this.successMessage.set(this.translate.instant('forgotPassword.codeSent'));
      this.currentStep.set(1);
    } catch (error: any) {
      this.isLoading.set(false);
      this.errorMessage.set(error?.detail || this.translate.instant('login.error'));
    }
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

    try {
      await firstValueFrom(
        this.accountService.resetPassword({
          email: this.email.value,
          code: this.code.value,
          newPassword,
        })
      );
      this.isLoading.set(false);
      this.successMessage.set(this.translate.instant('forgotPassword.resetSuccess'));
      setTimeout(() => {
        this.router.navigate(['/login']);
      }, 2000);
    } catch (error: any) {
      this.isLoading.set(false);
      this.errorMessage.set(error?.detail || this.translate.instant('login.error'));
    }
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }
}
