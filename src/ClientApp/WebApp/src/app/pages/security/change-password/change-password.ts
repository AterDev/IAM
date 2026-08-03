import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { AuthService } from 'src/app/services/auth.service';
import { AccountService } from 'src/app/services/account.service';
import { I18N_KEYS } from 'src/app/share/i18n-keys';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    TranslateModule,
  ],
  templateUrl: './change-password.html',
  styleUrl: './change-password.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangePasswordComponent {
  readonly i18n = I18N_KEYS;

  private readonly accountService = inject(AccountService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

  readonly isSaving = signal(false);
  readonly errorMessage = signal('');
  readonly successMessage = signal('');

  readonly form = new FormGroup({
    currentPassword: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(8), Validators.maxLength(100)],
    }),
    newPassword: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(8), Validators.maxLength(100)],
    }),
    confirmPassword: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  get currentPassword(): FormControl<string> {
    return this.form.get('currentPassword') as FormControl<string>;
  }

  get newPassword(): FormControl<string> {
    return this.form.get('newPassword') as FormControl<string>;
  }

  get confirmPassword(): FormControl<string> {
    return this.form.get('confirmPassword') as FormControl<string>;
  }

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.newPassword.value !== this.confirmPassword.value) {
      this.errorMessage.set(this.translate.instant(this.i18n.validation.passwordmismatch));
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    try {
      await firstValueFrom(this.accountService.changePassword({
        currentPassword: this.currentPassword.value,
        newPassword: this.newPassword.value,
      }));

      this.successMessage.set(this.translate.instant(this.i18n.changePassword.success));
      this.form.reset({ currentPassword: '', newPassword: '', confirmPassword: '' });
      this.isSaving.set(false);

      setTimeout(() => {
        this.authService.logoutFromServer();
      }, 1200);
    } catch (error: unknown) {
      const errorResult = error as { detail?: string } | null;
      this.isSaving.set(false);
      this.errorMessage.set(errorResult?.detail || this.translate.instant(this.i18n.changePassword.failed));
    }
  }

  cancel(): void {
    void this.router.navigate(['/user/list']);
  }
}
