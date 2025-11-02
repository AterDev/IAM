import { Component, inject, OnInit, AfterViewInit, ChangeDetectionStrategy } from '@angular/core';
import { FormControl, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { CommonModule } from '@angular/common';
import { AuthService } from 'src/app/services/auth.service';
import { I18N_KEYS } from 'src/app/share/i18n-keys';
import { initStarfield } from './starfield';
import { ApiClient } from 'src/app/services/api/api-client';
import { AdminLoginResponseDto } from 'src/app/services/api/models/identity-mod/admin-login-response-dto.model';
import { AdminLoginDto } from 'src/app/services/api/models/identity-mod/admin-login-dto.model';

@Component({
  selector: 'app-login',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    TranslateModule
  ],
  templateUrl: './login.html',
  styleUrls: ['./login.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Login implements OnInit, AfterViewInit {
  private authService = inject(AuthService);
  private apiClient = inject(ApiClient);
  private router = inject(Router);
  private translate = inject(TranslateService);

  public loginForm!: FormGroup;
  i18nKeys = I18N_KEYS;
  isLoading = false;
  errorMessage = '';

  ngAfterViewInit(): void {
    const canvas = document.getElementById('starfield') as HTMLCanvasElement | null;
    if (canvas) {
      initStarfield(canvas);
    }
  }

  get username() {
    return this.loginForm.get('username') as FormControl;
  }

  get password() {
    return this.loginForm.get('password') as FormControl;
  }

  ngOnInit(): void {
    this.authService.updateUserLoginState();
    if (this.authService.isLogin) {
      this.router.navigate(['/']);
    }

    this.loginForm = new FormGroup({
      username: new FormControl('', [
        Validators.required,
        Validators.minLength(4),
        Validators.maxLength(50)
      ]),
      password: new FormControl('', [
        Validators.required,
        Validators.minLength(6),
        Validators.maxLength(60)
      ])
    });
  }

  getValidatorMessage(control: FormControl | null): string {
    if (!control || !control.errors) {
      return '';
    }

    const errors = control.errors;
    const errorKeys = Object.keys(errors);
    if (errorKeys.length === 0) {
      return '';
    }

    const key = errorKeys[0];
    const params = errors[key];
    const translationKey = `validation.${key.toLowerCase()}`;
    return this.translate.instant(translationKey, params);
  }

  async doLogin(): Promise<void> {
    if (this.loginForm.invalid) {
      return;
    }
    this.isLoading = true;
    this.errorMessage = '';
    const { username, password } = this.loginForm.value;

    try {
      const dto: AdminLoginDto = { userName: username, password };
      const response = await new Promise<AdminLoginResponseDto>((resolve, reject) => {
        this.apiClient.adminAuth.login(dto).subscribe({
          next: (res) => resolve(res),
          error: (err) => reject(err)
        });
      });

      // Save login state via AuthService
      if (response && response.accessToken) {
        this.authService.saveLoginState(response.user?.userName || username, response.accessToken);
        this.router.navigate(['/user']);
      } else {
        this.errorMessage = this.translate.instant('login.failed');
      }
    } catch (error) {
      this.errorMessage = this.translate.instant('login.error');
      console.error('Login error:', error);
    } finally {
      this.isLoading = false;
    }
  }

  goToRegister(): void {
    this.router.navigate(['/register']);
  }

  goToForgotPassword(): void {
    this.router.navigate(['/forgot-password']);
  }
}
