import { Component, OnInit } from '@angular/core';
import { CommonModules, CommonFormModules } from 'src/app/share/shared-modules';
import { FormBuilder, FormGroup, FormControl, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { ApiClient } from 'src/app/services/api/api-client';
import { RoleAddDto } from 'src/app/services/api/models/identity-mod/role-add-dto.model';

@Component({
  selector: 'app-role-add',
  imports: [
    ...CommonModules,
    ...CommonFormModules,
    MatDialogModule
  ],
  templateUrl: './role-add.html',
  styleUrls: ['./role-add.scss']
})
export class RoleAddComponent implements OnInit {
  roleForm!: FormGroup;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private api: ApiClient,
    private dialogRef: MatDialogRef<RoleAddComponent>,
    private snackBar: MatSnackBar,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.roleForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      description: ['']
    });
  }

  get name() {
    return this.roleForm.get('name') as FormControl;
  }

  get description() {
    return this.roleForm.get('description') as FormControl;
  }

  onSubmit(): void {
    if (this.roleForm.valid && !this.isSubmitting) {
      this.isSubmitting = true;
      
      const data: RoleAddDto = {
        name: this.roleForm.value.name,
        description: this.roleForm.value.description || null
      };

      this.api.roles.createRole(data).subscribe({
        next: () => {
          this.snackBar.open(
            this.translate.instant('success.roleCreated'),
            this.translate.instant('common.close'),
            { duration: 3000 }
          );
          this.dialogRef.close(true);
        },
        error: (error) => {
          console.error('Failed to create role:', error);
          this.snackBar.open(
            this.translate.instant('error.createRoleFailed'),
            this.translate.instant('common.close'),
            { duration: 3000 }
          );
          this.isSubmitting = false;
        }
      });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
