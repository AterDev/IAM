import { Component, OnInit, Inject } from '@angular/core';
import { CommonModules, CommonFormModules } from 'src/app/share/shared-modules';
import { FormBuilder, FormGroup, FormControl, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiClient } from 'src/app/services/api/api-client';
import { RoleItemDto } from 'src/app/services/api/models/identity-mod/role-item-dto.model';
import { RoleUpdateDto } from 'src/app/services/api/models/identity-mod/role-update-dto.model';

@Component({
  selector: 'app-role-edit',
  imports: [
    ...CommonModules,
    ...CommonFormModules,
    MatDialogModule
  ],
  templateUrl: './role-edit.html',
  styleUrls: ['./role-edit.scss']
})
export class RoleEditComponent implements OnInit {
  roleForm!: FormGroup;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private api: ApiClient,
    private dialogRef: MatDialogRef<RoleEditComponent>,
    private snackBar: MatSnackBar,
    @Inject(MAT_DIALOG_DATA) public data: RoleItemDto
  ) {}

  ngOnInit(): void {
    this.roleForm = this.fb.group({
      name: [this.data.name, [Validators.required, Validators.minLength(2)]],
      description: [this.data.description || '']
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
      
      const updateData: RoleUpdateDto = {
        name: this.roleForm.value.name,
        description: this.roleForm.value.description || null
      };

      this.api.roles.updateRole(this.data.id, updateData).subscribe({
        next: () => {
          this.snackBar.open('角色更新成功', '关闭', { duration: 3000 });
          this.dialogRef.close(true);
        },
        error: (error) => {
          console.error('Failed to update role:', error);
          this.snackBar.open('更新角色失败', '关闭', { duration: 3000 });
          this.isSubmitting = false;
        }
      });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
