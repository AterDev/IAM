# Angular Coding Standards for IAM Project

This document outlines coding standards and best practices for Angular development in the IAM project. All developers and AI assistants (like Copilot) must follow these guidelines.

## 1. API Service Usage

### ❌ DON'T: Import individual service files
```typescript
import { UsersService } from 'src/app/services/api/services/users.service';
import { OrganizationsService } from 'src/app/services/api/services/organizations.service';

constructor(
  private usersService: UsersService,
  private organizationsService: OrganizationsService
) {}
```

### ✅ DO: Use the unified ApiClient
```typescript
import { ApiClient } from 'src/app/services/api/api-client';

constructor(
  private api: ApiClient
) {}

// Access services through api
this.api.users.getUsers(...)
this.api.organizations.getTree(...)
```

**Benefits:**
- Single injection point for all API services
- Easier to mock in tests
- Clearer dependency management
- Consistent API access pattern

## 2. Signal Usage

### ❌ DON'T: Overuse signals for non-reactive properties
```typescript
export class MyComponent {
  pageSize = signal(10);          // Not needed - not reactive in template
  pageIndex = signal(0);           // Not needed - not reactive in template
  isLoading = signal(false);       // Not needed if only used once
  searchText = signal('');         // Not needed if bound to ngModel
}
```

### ✅ DO: Use signals only when necessary for reactivity
```typescript
export class MyComponent {
  // Use signals for values displayed in templates with ()
  dataSource = signal<User[]>([]);
  total = signal(0);
  selectedIds = signal<Set<string>>(new Set());
  
  // Use computed for derived reactive values
  allSelected = computed(() => {
    const data = this.dataSource();
    const selected = this.selectedIds();
    return data.length > 0 && data.every(item => selected.has(item.id));
  });
  
  // Use regular properties for non-reactive values
  pageSize = 10;
  pageIndex = 0;
  isLoading = false;
  searchText = '';
}
```

**Guidelines:**
- Use signals when the value is displayed in templates and needs reactivity
- Use signals when other computed signals depend on the value
- Use regular properties for simple state that doesn't need reactivity
- Use computed() for derived values based on signals

## 3. Form Control Binding

### ❌ DON'T: Use formControlName with string literals
```typescript
// Component
export class MyForm {
  myForm!: FormGroup;
}

// Template
<input matInput formControlName="userName">
<mat-error *ngIf="myForm.get('userName')?.invalid">
  {{ getErrorMessage('userName') }}
</mat-error>
```

### ✅ DO: Use [formControl] with getter properties
```typescript
// Component
import { FormControl } from '@angular/forms';

export class MyForm {
  myForm!: FormGroup;
  
  get userName() {
    return this.myForm.get('userName') as FormControl;
  }
  
  get email() {
    return this.myForm.get('email') as FormControl;
  }
}

// Template
<input matInput [formControl]="userName">
<mat-error *ngIf="userName.invalid && userName.touched">
  {{ getErrorMessage(userName) }}
</mat-error>
```

**Benefits:**
- Type safety - TypeScript will catch typos
- No hardcoded string literals
- Easier refactoring
- Better IDE autocomplete
- Centralized form control access

## 4. Form Control Getters

### ❌ DON'T: Pass field names as strings
```typescript
getErrorMessage(fieldName: string): string {
  const field = this.myForm.get(fieldName);
  if (field?.hasError('required')) {
    return 'This field is required';
  }
  return '';
}
```

### ✅ DO: Use typed getters and pass FormControl
```typescript
// Define getters for each form control
get userName() {
  return this.myForm.get('userName') as FormControl;
}

get email() {
  return this.myForm.get('email') as FormControl;
}

// Method accepts FormControl directly
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

// Usage in template
{{ getErrorMessage(userName) }}
{{ getErrorMessage(email) }}
```

**Benefits:**
- Type-safe form control access
- No string literals in error checking
- Reusable error message method
- Better compile-time checking

## 5. Complete Example

Here's a complete example following all standards:

```typescript
// user-add.component.ts
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormControl, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiClient } from 'src/app/services/api/api-client';
import { UserAddDto } from 'src/app/services/api/models/identity-mod/user-add-dto.model';

@Component({
  selector: 'app-user-add',
  templateUrl: './user-add.html',
  styleUrls: ['./user-add.scss']
})
export class UserAddComponent implements OnInit {
  userForm!: FormGroup;
  hidePassword = true;
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
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  // Getters for form controls
  get userName() {
    return this.userForm.get('userName') as FormControl;
  }

  get email() {
    return this.userForm.get('email') as FormControl;
  }

  get password() {
    return this.userForm.get('password') as FormControl;
  }

  onSubmit(): void {
    if (this.userForm.invalid) {
      Object.keys(this.userForm.controls).forEach(key => {
        this.userForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.isSubmitting = true;
    const dto: UserAddDto = {
      userName: this.userForm.value.userName,
      email: this.userForm.value.email || null,
      password: this.userForm.value.password
    };

    this.api.users.createUser(dto).subscribe({
      next: () => {
        this.snackBar.open('User created successfully', 'Close', { duration: 3000 });
        this.dialogRef.close(true);
      },
      error: (error) => {
        this.isSubmitting = false;
        this.snackBar.open('Failed to create user', 'Close', { duration: 3000 });
      }
    });
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
```

```html
<!-- user-add.html -->
<form [formGroup]="userForm">
  <mat-form-field appearance="outline">
    <mat-label>Username</mat-label>
    <input matInput [formControl]="userName" required>
    <mat-error *ngIf="userName.invalid && userName.touched">
      {{ getErrorMessage(userName) }}
    </mat-error>
  </mat-form-field>

  <mat-form-field appearance="outline">
    <mat-label>Email</mat-label>
    <input matInput type="email" [formControl]="email">
    <mat-error *ngIf="email.invalid && email.touched">
      {{ getErrorMessage(email) }}
    </mat-error>
  </mat-form-field>

  <mat-form-field appearance="outline">
    <mat-label>Password</mat-label>
    <input matInput [type]="hidePassword ? 'password' : 'text'" [formControl]="password" required>
    <mat-error *ngIf="password.invalid && password.touched">
      {{ getErrorMessage(password) }}
    </mat-error>
  </mat-form-field>
</form>
```

## 6. Additional Best Practices

### Import Organization
```typescript
// Angular core imports
import { Component, OnInit } from '@angular/core';

// Angular forms
import { FormBuilder, FormGroup, FormControl, Validators } from '@angular/forms';

// Angular Material
import { MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';

// Application services
import { ApiClient } from 'src/app/services/api/api-client';

// Application models
import { UserAddDto } from 'src/app/services/api/models/identity-mod/user-add-dto.model';
```

### Consistent Naming
- Component files: `user-add.ts`, `user-edit.ts`, `organization-list.ts`
- Component classes: `UserAddComponent`, `UserEditComponent`, `OrganizationListComponent`
- Form groups: `userForm`, `orgForm`, etc.
- Form getters: Match field names (e.g., `userName`, `email`, `displayOrder`)

### Error Handling
- Always provide user feedback for errors
- Use consistent error messages
- Show success feedback for actions
- Use MatSnackBar for notifications

## 7. Migration Checklist

When updating existing code to follow these standards:

- [ ] Replace individual service injections with `ApiClient`
- [ ] Review signal usage - keep only necessary signals
- [ ] Add getters for all form controls
- [ ] Update templates to use `[formControl]` instead of `formControlName`
- [ ] Update error message methods to accept `FormControl` instead of strings
- [ ] Update templates to use getters (e.g., `userName.invalid` instead of `myForm.get('userName')?.invalid`)
- [ ] Test thoroughly after changes

## Summary

These standards ensure:
1. **Consistency** across the codebase
2. **Type safety** with fewer runtime errors
3. **Maintainability** with easier refactoring
4. **Better DX** (Developer Experience) with autocomplete and compile-time checks

All developers and AI assistants must follow these guidelines for new code and when modifying existing code.
