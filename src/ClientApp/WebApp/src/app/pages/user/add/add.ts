import { Component, OnInit } from '@angular/core';
import { CommonModules, CommonFormModules } from 'src/app/share/shared-modules';
import { FormBuilder, FormGroup, FormControl, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiClient } from 'src/app/services/api/api-client';
import { UserAddDto } from 'src/app/services/api/models/iammod/user-add-dto.model';
import { I18N_KEYS } from 'src/app/share/i18n-keys';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-add',
  imports: [
    ...CommonModules,
    ...CommonFormModules,
    MatDialogModule
  ],
  templateUrl: './add.html',
  styleUrls: ['./add.scss']
})
export class UserAddComponent implements OnInit {
  readonly i18n = I18N_KEYS;
  userForm!: FormGroup;
  hidePassword = true;
  hideConfirmPassword = true;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private api: ApiClient,
    private dialogRef: MatDialogRef<UserAddComponent>,
    private snackBar: MatSnackBar,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.userForm = this.fb.group({
      userName: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: [''],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });
  }

  get userName() {
    return this.userForm.get('userName') as FormControl;
  }

  get email() {
    return this.userForm.get('email') as FormControl;
  }

  get phoneNumber() {
    return this.userForm.get('phoneNumber') as FormControl;
  }

  get password() {
    return this.userForm.get('password') as FormControl;
  }

  get confirmPassword() {
    return this.userForm.get('confirmPassword') as FormControl;
  }

  passwordMatchValidator(group: FormGroup): { [key: string]: boolean } | null {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  onSubmit(): void {
    if (this.userForm.invalid) {
      Object.keys(this.userForm.controls).forEach(key => {
        this.userForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.isSubmitting = true;
    const formValue = this.userForm.value;
    const dto: UserAddDto = {
      userName: formValue.userName,
      email: formValue.email,
      phoneNumber: formValue.phoneNumber || null,
      password: formValue.password,
      emailConfirmed: false,
      phoneNumberConfirmed: false,
      lockoutEnabled: false
    };

    this.api.users.createUser(dto).subscribe({
      next: () => {
        this.snackBar.open(this.translate.instant(this.i18n.user.createdSuccess), this.translate.instant(this.i18n.common.close), { duration: 3000 });
        this.dialogRef.close(true);
      },
      error: (error) => {
        this.isSubmitting = false;
        const errorMsg = error?.error?.message || this.translate.instant(this.i18n.user.createFailed);
        this.snackBar.open(errorMsg, this.translate.instant(this.i18n.common.close), { duration: 3000 });
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  getErrorMessage(control: FormControl, fieldName: string): string {
    if (control?.hasError('required')) {
      return this.translate.instant(this.i18n.validation.required);
    }
    if (control?.hasError('email')) {
      return this.translate.instant(this.i18n.user.invalidEmail);
    }
    if (control?.hasError('minlength')) {
      const minLength = control.errors?.['minlength'].requiredLength;
      return this.translate.instant(this.i18n.validation.minlength, { requiredLength: minLength });
    }
    if (fieldName === 'confirmPassword' && this.userForm.hasError('passwordMismatch')) {
      return this.translate.instant(this.i18n.user.passwordMismatch);
    }
    return '';
  }
}
