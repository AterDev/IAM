import { Component, OnInit } from '@angular/core';
import { CommonModules, CommonFormModules } from 'src/app/share/shared-modules';
import { FormBuilder, FormGroup, FormControl, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiClient } from 'src/app/services/api/api-client';
import { UserAddDto } from 'src/app/services/api/models/identity-mod/user-add-dto.model';

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
  userForm!: FormGroup;
  hidePassword = true;
  hideConfirmPassword = true;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private api: ApiClient,
    private dialogRef: MatDialogRef<UserAddComponent>,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.userForm = this.fb.group({
      userName: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.email]],
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
      email: formValue.email || null,
      phoneNumber: formValue.phoneNumber || null,
      password: formValue.password,
      emailConfirmed: false,
      phoneNumberConfirmed: false,
      lockoutEnabled: false
    };

    this.api.users.createUser(dto).subscribe({
      next: () => {
        this.snackBar.open('User created successfully', 'Close', { duration: 3000 });
        this.dialogRef.close(true);
      },
      error: (error) => {
        this.isSubmitting = false;
        const errorMsg = error?.error?.message || 'Failed to create user';
        this.snackBar.open(errorMsg, 'Close', { duration: 3000 });
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  getErrorMessage(control: FormControl, fieldName: string): string {
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
    if (fieldName === 'confirmPassword' && this.userForm.hasError('passwordMismatch')) {
      return 'Passwords do not match';
    }
    return '';
  }
}
