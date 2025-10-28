import { Component, OnInit, Inject } from '@angular/core';
import { CommonModules, CommonFormModules } from 'src/app/share/shared-modules';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { OrganizationsService } from 'src/app/services/api/services/organizations.service';
import { OrganizationUpdateDto } from 'src/app/services/api/models/identity-mod/organization-update-dto.model';
import { OrganizationDetailDto } from 'src/app/services/api/models/identity-mod/organization-detail-dto.model';

@Component({
  selector: 'app-organization-edit',
  imports: [
    ...CommonModules,
    ...CommonFormModules,
    MatDialogModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './organization-edit.html',
  styleUrls: ['./organization-edit.scss']
})
export class OrganizationEditComponent implements OnInit {
  orgForm!: FormGroup;
  isSubmitting = false;
  isLoading = true;
  organization?: OrganizationDetailDto;

  constructor(
    private fb: FormBuilder,
    private organizationsService: OrganizationsService,
    private dialogRef: MatDialogRef<OrganizationEditComponent>,
    private snackBar: MatSnackBar,
    @Inject(MAT_DIALOG_DATA) public data: { organizationId: string }
  ) {}

  ngOnInit(): void {
    this.orgForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      description: [''],
      displayOrder: [0, [Validators.min(0)]]
    });

    this.loadOrganization();
  }

  loadOrganization(): void {
    this.organizationsService.getDetail(this.data.organizationId).subscribe({
      next: (org) => {
        this.organization = org;
        this.orgForm.patchValue({
          name: org.name,
          description: org.description || '',
          displayOrder: org.displayOrder
        });
        this.isLoading = false;
      },
      error: () => {
        this.snackBar.open('Failed to load organization', 'Close', { duration: 3000 });
        this.dialogRef.close(false);
      }
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
    const dto: OrganizationUpdateDto = {
      name: formValue.name,
      description: formValue.description || null,
      displayOrder: formValue.displayOrder
    };

    this.organizationsService.updateOrganization(this.data.organizationId, dto).subscribe({
      next: () => {
        this.snackBar.open('Organization updated successfully', 'Close', { duration: 3000 });
        this.dialogRef.close(true);
      },
      error: (error) => {
        this.isSubmitting = false;
        const errorMsg = error?.error?.message || 'Failed to update organization';
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
