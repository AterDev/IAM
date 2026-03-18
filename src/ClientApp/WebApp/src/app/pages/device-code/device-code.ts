import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, ChangeDetectionStrategy, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { AuthService } from 'src/app/services/auth.service';
import { DeviceAuthorizationInteraction, OauthInteractionService } from 'src/app/services/oauth-interaction.service';

@Component({
  selector: 'app-device-code',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatProgressSpinnerModule,
    TranslateModule
  ],
  templateUrl: './device-code.html',
  styleUrls: ['./device-code.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DeviceCode implements OnInit {
  readonly authService = inject(AuthService);

  private destroyRef = inject(DestroyRef);
  private interactionService = inject(OauthInteractionService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private translate = inject(TranslateService);

  deviceCodeForm = new FormGroup({
    userCode: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[A-Z0-9]{4}-[A-Z0-9]{4}$/)]
    })
  });

  isLoading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');
  interaction = signal<DeviceAuthorizationInteraction | null>(null);

  ngOnInit(): void {
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        const userCode = this.normalizeUserCode(params.get('user_code') ?? '');

        if (!userCode) {
          return;
        }

        this.userCode.setValue(userCode, { emitEvent: false });

        if (this.authService.isAuthenticated()) {
          this.lookupInteraction(userCode);
        }
      });
  }

  get userCode() {
    return this.deviceCodeForm.get('userCode') as FormControl;
  }

  formatUserCode(event: Event): void {
    const input = event.target as HTMLInputElement;
    let value = input.value.toUpperCase().replace(/[^A-Z0-9]/g, '');

    if (value.length > 4) {
      value = value.slice(0, 4) + '-' + value.slice(4, 8);
    }

    input.value = value;
    this.userCode.setValue(value, { emitEvent: false });
  }

  async submitCode(): Promise<void> {
    if (this.deviceCodeForm.invalid) {
      this.userCode.markAsTouched();
      return;
    }

    const userCode = this.normalizeUserCode(this.userCode.value);
    this.userCode.setValue(userCode, { emitEvent: false });
    this.lookupInteraction(userCode, true);
  }

  cancel(): void {
    this.router.navigate(['/']);
  }

  approve(): void {
    this.submitDecision(true);
  }

  deny(): void {
    this.submitDecision(false);
  }

  private lookupInteraction(userCode: string, allowLoginRedirect = false): void {
    this.isLoading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.interactionService.getDeviceInteraction(userCode)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: interaction => {
          this.isLoading.set(false);
          this.applyInteraction(interaction);

          if (interaction.status === 'pending' && allowLoginRedirect && !this.authService.isAuthenticated()) {
            this.router.navigate(['/login'], {
              queryParams: {
                returnUrl: this.router.createUrlTree(['/device-code'], {
                  queryParams: { user_code: interaction.userCode }
                }).toString()
              }
            });
          }
        },
        error: (error: HttpErrorResponse) => {
          this.isLoading.set(false);
          this.interaction.set(null);
          this.errorMessage.set(
            error.status === 400
              ? this.translate.instant('deviceCode.invalidCode')
              : this.translate.instant('deviceCode.loadError')
          );
        }
      });
  }

  private submitDecision(approve: boolean): void {
    const current = this.interaction();
    if (!current || current.status !== 'pending') {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.interactionService.submitDeviceDecision({
      userCode: current.userCode,
      approve
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: interaction => {
          this.isLoading.set(false);
          this.applyInteraction(interaction);
        },
        error: () => {
          this.isLoading.set(false);
          this.errorMessage.set(this.translate.instant('deviceCode.decisionError'));
        }
      });
  }

  private applyInteraction(interaction: DeviceAuthorizationInteraction): void {
    this.interaction.set(interaction);
    this.userCode.setValue(interaction.userCode, { emitEvent: false });

    switch (interaction.status) {
      case 'approved':
        this.successMessage.set(this.translate.instant('deviceCode.success'));
        this.errorMessage.set('');
        break;
      case 'denied':
        this.successMessage.set('');
        this.errorMessage.set(this.translate.instant('deviceCode.denied'));
        break;
      case 'expired':
        this.successMessage.set('');
        this.errorMessage.set(this.translate.instant('deviceCode.expired'));
        break;
      case 'invalid':
        this.successMessage.set('');
        this.errorMessage.set(this.translate.instant('deviceCode.invalidCode'));
        break;
      default:
        this.successMessage.set('');
        this.errorMessage.set('');
        break;
    }
  }

  private normalizeUserCode(value: string): string {
    const alphanumeric = value.toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, 8);
    return alphanumeric.length > 4
      ? `${alphanumeric.slice(0, 4)}-${alphanumeric.slice(4)}`
      : alphanumeric;
  }
}
