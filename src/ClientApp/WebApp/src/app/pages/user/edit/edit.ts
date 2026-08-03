import { Component, OnInit, Inject, signal } from '@angular/core';
import { CommonModules, CommonFormModules } from 'src/app/share/shared-modules';
import { FormBuilder, FormGroup, FormControl, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiClient } from 'src/app/services/api/api-client';
import { UserUpdateDto } from 'src/app/services/api/models/iammod/user-update-dto.model';
import { UserDetailDto } from 'src/app/services/api/models/iammod/user-detail-dto.model';
import { I18N_KEYS } from 'src/app/share/i18n-keys';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-edit',
  imports: [
    ...CommonModules,
    ...CommonFormModules,
    MatDialogModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './edit.html',
  styleUrls: ['./edit.scss']
})
export class UserEditComponent implements OnInit {
  readonly i18n = I18N_KEYS;
  userForm!: FormGroup;
  isSubmitting = false;
  isLoading = signal(true);
  user?: UserDetailDto;

  constructor(
    private fb: FormBuilder,
    private api: ApiClient,
    private dialogRef: MatDialogRef<UserEditComponent>,
    private snackBar: MatSnackBar,
    private translate: TranslateService,
    @Inject(MAT_DIALOG_DATA) public data: { userId: string }
  ) {}

  ngOnInit(): void {
    this.userForm = this.fb.group({
      email: ['', [Validators.email]],
      phoneNumber: ['']
    });

    this.loadUser();
  }

  get email() {
    return this.userForm.get('email') as FormControl;
  }

  get phoneNumber() {
    return this.userForm.get('phoneNumber') as FormControl;
  }

  loadUser(): void {
    this.api.users.getDetail(this.data.userId).subscribe({
      next: (user) => {
        this.user = user;
        this.userForm.patchValue({
          email: user.email || '',
          phoneNumber: user.phoneNumber || ''
        });
  this.isLoading.set(false);
      },
      error: () => {
        this.snackBar.open(this.translate.instant(this.i18n.user.loadFailed), this.translate.instant(this.i18n.common.close), { duration: 3000 });
        this.dialogRef.close(false);
      }
    });
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
    const dto: UserUpdateDto = {
      email: formValue.email || null,
      phoneNumber: formValue.phoneNumber || null
    };

    this.api.users.updateUser(this.data.userId, dto).subscribe({
      next: () => {
        this.snackBar.open(this.translate.instant(this.i18n.user.updatedSuccess), this.translate.instant(this.i18n.common.close), { duration: 3000 });
        this.dialogRef.close(true);
      },
      error: (error) => {
        this.isSubmitting = false;
        const errorMsg = error?.error?.message || this.translate.instant(this.i18n.user.updateFailed);
        this.snackBar.open(errorMsg, this.translate.instant(this.i18n.common.close), { duration: 3000 });
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  getErrorMessage(control: FormControl): string {
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
    return '';
  }
}
