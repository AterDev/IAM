import { Component, OnInit, Inject } from '@angular/core';
import { CommonModules, CommonFormModules } from 'src/app/share/shared-modules';
import { FormBuilder, FormGroup, FormControl, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiClient } from 'src/app/services/api/api-client';
import { UserUpdateDto } from 'src/app/services/api/models/identity-mod/user-update-dto.model';
import { UserDetailDto } from 'src/app/services/api/models/identity-mod/user-detail-dto.model';

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
  userForm!: FormGroup;
  isSubmitting = false;
  isLoading = true;
  user?: UserDetailDto;

  constructor(
    private fb: FormBuilder,
    private api: ApiClient,
    private dialogRef: MatDialogRef<UserEditComponent>,
    private snackBar: MatSnackBar,
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
        this.isLoading = false;
      },
      error: () => {
        this.snackBar.open('Failed to load user', 'Close', { duration: 3000 });
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
        this.snackBar.open('User updated successfully', 'Close', { duration: 3000 });
        this.dialogRef.close(true);
      },
      error: (error) => {
        this.isSubmitting = false;
        const errorMsg = error?.error?.message || 'Failed to update user';
        this.snackBar.open(errorMsg, 'Close', { duration: 3000 });
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  getErrorMessage(control: FormControl): string {
    if (control?.hasError('required')) {
      return 'This field is required';
    }
    if (control?.hasError('email')) {
      return 'Please enter a valid email';
    }
    if (control?.hasError('minlength')) {
      const minLength = control.errors?.['minlength'].requiredLength;
      return `Minimum length is ${minLength}`;
    }
    return '';
  }
}
