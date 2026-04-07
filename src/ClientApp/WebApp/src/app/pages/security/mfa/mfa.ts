import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import QRCode from 'qrcode';
import { MfaService, MfaSetupResponse, MfaStatus } from 'src/app/services/mfa.service';
import { I18N_KEYS } from 'src/app/share/i18n-keys';

@Component({
  selector: 'app-mfa-settings',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatDividerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatListModule,
    TranslateModule,
  ],
  templateUrl: './mfa.html',
  styleUrl: './mfa.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MfaSettingsComponent {
  readonly i18n = I18N_KEYS;

  private readonly destroyRef = inject(DestroyRef);
  private readonly mfaService = inject(MfaService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);

  readonly status = signal<MfaStatus | null>(null);
  readonly setup = signal<MfaSetupResponse | null>(null);
  readonly recoveryCodes = signal<string[]>([]);
  readonly qrCodeDataUrl = signal<string | null>(null);
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly errorMessage = signal('');

  readonly setupForm = new FormGroup({
    code: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.pattern(/^\d{6}$/)] }),
  });

  readonly manageForm = new FormGroup({
    code: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  constructor() {
    this.loadStatus();
  }

  get setupCode(): FormControl<string> {
    return this.setupForm.get('code') as FormControl<string>;
  }

  get manageCode(): FormControl<string> {
    return this.manageForm.get('code') as FormControl<string>;
  }

  loadStatus(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.mfaService.getStatus()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (status) => {
          this.status.set(status);
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
          this.errorMessage.set(this.translate.instant('mfa.loadFailed'));
        },
      });
  }

  startSetup(): void {
    this.isSaving.set(true);
    this.errorMessage.set('');
    this.recoveryCodes.set([]);

    this.mfaService.beginSetup()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.setup.set(response);
          void this.renderQrCode(response.otpAuthUri);
          this.isSaving.set(false);
          this.snackBar.open(this.translate.instant('mfa.setupReady'), undefined, { duration: 3000 });
          this.loadStatus();
        },
        error: () => {
          this.isSaving.set(false);
          this.errorMessage.set(this.translate.instant('mfa.setupFailed'));
        },
      });
  }

  enable(): void {
    if (this.setupForm.invalid) {
      this.setupCode.markAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set('');

    this.mfaService.enable(this.setupCode.value)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.recoveryCodes.set(response.recoveryCodes);
          this.setup.set(null);
          this.qrCodeDataUrl.set(null);
          this.setupForm.reset({ code: '' });
          this.isSaving.set(false);
          this.snackBar.open(this.translate.instant('mfa.enabled'), undefined, { duration: 3000 });
          this.loadStatus();
        },
        error: () => {
          this.isSaving.set(false);
          this.errorMessage.set(this.translate.instant('mfa.enableFailed'));
        },
      });
  }

  disable(): void {
    if (this.manageForm.invalid) {
      this.manageCode.markAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set('');

    this.mfaService.disable(this.manageCode.value)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.recoveryCodes.set([]);
          this.setup.set(null);
          this.qrCodeDataUrl.set(null);
          this.manageForm.reset({ code: '' });
          this.isSaving.set(false);
          this.snackBar.open(this.translate.instant('mfa.disabled'), undefined, { duration: 3000 });
          this.loadStatus();
        },
        error: () => {
          this.isSaving.set(false);
          this.errorMessage.set(this.translate.instant('mfa.disableFailed'));
        },
      });
  }

  regenerateRecoveryCodes(): void {
    if (this.manageForm.invalid) {
      this.manageCode.markAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set('');

    this.mfaService.regenerateRecoveryCodes(this.manageCode.value)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.recoveryCodes.set(response.recoveryCodes);
          this.manageForm.reset({ code: '' });
          this.isSaving.set(false);
          this.snackBar.open(this.translate.instant('mfa.recoveryCodesRegenerated'), undefined, { duration: 3000 });
          this.loadStatus();
        },
        error: () => {
          this.isSaving.set(false);
          this.errorMessage.set(this.translate.instant('mfa.regenerateFailed'));
        },
      });
  }

  copy(text: string, messageKey: string): void {
    navigator.clipboard.writeText(text).then(() => {
      this.snackBar.open(this.translate.instant(messageKey), undefined, { duration: 2500 });
    });
  }

  private async renderQrCode(uri: string): Promise<void> {
    try {
      const dataUrl = await QRCode.toDataURL(uri, {
        errorCorrectionLevel: 'M',
        margin: 1,
        width: 220,
        color: {
          dark: '#111827',
          light: '#FFFFFFFF',
        },
      });
      this.qrCodeDataUrl.set(dataUrl);
    } catch {
      this.qrCodeDataUrl.set(null);
    }
  }
}
