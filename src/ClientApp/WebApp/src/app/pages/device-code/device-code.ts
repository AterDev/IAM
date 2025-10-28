import { Component, inject, OnInit, ChangeDetectionStrategy, signal } from '@angular/core';
import { FormControl, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { CommonModule } from '@angular/common';
import { ApiClient } from 'src/app/services/api/api-client';
import { OidcAuthService } from 'src/app/services/oidc-auth.service';

@Component({
  selector: 'app-device-code',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    TranslateModule
  ],
  templateUrl: './device-code.html',
  styleUrls: ['./device-code.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DeviceCode implements OnInit {
  private apiClient = inject(ApiClient);
  private authService = inject(OidcAuthService);
  private router = inject(Router);
  private translate = inject(TranslateService);

  deviceCodeForm!: FormGroup;
  
  isLoading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  ngOnInit(): void {
    this.deviceCodeForm = new FormGroup({
      userCode: new FormControl('', [
        Validators.required,
        Validators.pattern(/^[A-Z0-9]{4}-[A-Z0-9]{4}$/)
      ])
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
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    const userCode = this.userCode.value;

    // TODO: Implement device authorization API
    // For now, simulate the flow
    try {
      // In a real implementation, this would call the device authorization endpoint
      setTimeout(() => {
        if (this.authService.isAuthenticated()) {
          this.successMessage.set(this.translate.instant('deviceCode.success'));
          setTimeout(() => {
            this.router.navigate(['/']);
          }, 2000);
        } else {
          // Redirect to login with device code
          this.router.navigate(['/login'], {
            queryParams: { device_code: userCode }
          });
        }
        this.isLoading.set(false);
      }, 1000);
    } catch (error) {
      this.errorMessage.set(this.translate.instant('deviceCode.invalidCode'));
      this.isLoading.set(false);
    }
  }

  cancel(): void {
    this.router.navigate(['/']);
  }
}
