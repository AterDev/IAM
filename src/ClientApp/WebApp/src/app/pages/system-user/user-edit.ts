import { Component, OnInit, Inject } from '@angular/core';
import { CommonModules, CommonFormModules } from 'src/app/share/shared-modules';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { UsersService } from 'src/app/services/api/services/users.service';
import { UserUpdateDto } from 'src/app/services/api/models/identity-mod/user-update-dto.model';
import { UserDetailDto } from 'src/app/services/api/models/identity-mod/user-detail-dto.model';

@Component({
  selector: 'app-user-edit',
  imports: [
    ...CommonModules,
    ...CommonFormModules,
    MatDialogModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './user-edit.html',
  styleUrls: ['./user-edit.scss']
})
export class UserEditComponent implements OnInit {
  userForm!: FormGroup;
  isSubmitting = false;
  isLoading = true;
  user?: UserDetailDto;

  constructor(
    private fb: FormBuilder,
    private usersService: UsersService,
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

  loadUser(): void {
    this.usersService.getDetail(this.data.userId).subscribe({
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

    this.usersService.updateUser(this.data.userId, dto).subscribe({
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

  getErrorMessage(fieldName: string): string {
    const field = this.userForm.get(fieldName);
    if (field?.hasError('required')) {
      return 'This field is required';
    }
    if (field?.hasError('email')) {
      return 'Please enter a valid email';
    }
    if (field?.hasError('minlength')) {
      const minLength = field.errors?.['minlength'].requiredLength;
      return `Minimum length is ${minLength}`;
    }
    return '';
  }
}
