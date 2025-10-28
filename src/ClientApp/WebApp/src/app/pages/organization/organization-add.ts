import { Component, OnInit, Inject } from '@angular/core';
import { CommonModules, CommonFormModules } from 'src/app/share/shared-modules';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { OrganizationsService } from 'src/app/services/api/services/organizations.service';
import { OrganizationAddDto } from 'src/app/services/api/models/identity-mod/organization-add-dto.model';

@Component({
  selector: 'app-organization-add',
  imports: [
    ...CommonModules,
    ...CommonFormModules,
    MatDialogModule
  ],
  templateUrl: './organization-add.html',
  styleUrls: ['./organization-add.scss']
})
export class OrganizationAddComponent implements OnInit {
  orgForm!: FormGroup;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private organizationsService: OrganizationsService,
    private dialogRef: MatDialogRef<OrganizationAddComponent>,
    private snackBar: MatSnackBar,
    @Inject(MAT_DIALOG_DATA) public data: { parentId: string | null }
  ) {}

  ngOnInit(): void {
    this.orgForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      description: [''],
      displayOrder: [0, [Validators.min(0)]]
    });
  }

  onSubmit(): void {
    if (this.orgForm.invalid) {
      Object.keys(this.orgForm.controls).forEach(key => {
        this.orgForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.isSubmitting = true;
    const formValue = this.orgForm.value;
    const dto: OrganizationAddDto = {
      name: formValue.name,
      parentId: this.data.parentId,
      description: formValue.description || null,
      displayOrder: formValue.displayOrder
    };

    this.organizationsService.createOrganization(dto).subscribe({
      next: () => {
        this.snackBar.open('Organization created successfully', 'Close', { duration: 3000 });
        this.dialogRef.close(true);
      },
      error: (error) => {
        this.isSubmitting = false;
        const errorMsg = error?.error?.message || 'Failed to create organization';
        this.snackBar.open(errorMsg, 'Close', { duration: 3000 });
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  getErrorMessage(fieldName: string): string {
    const field = this.orgForm.get(fieldName);
    if (field?.hasError('required')) {
      return 'This field is required';
    }
    if (field?.hasError('minlength')) {
      const minLength = field.errors?.['minlength'].requiredLength;
      return `Minimum length is ${minLength}`;
    }
    if (field?.hasError('min')) {
      return 'Value must be greater than or equal to 0';
    }
    return '';
  }
}
