import { Component, OnInit, Inject, signal } from '@angular/core';
import { CommonModules, CommonFormModules } from 'src/app/share/shared-modules';
import { FormBuilder, FormGroup, FormControl, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiClient } from 'src/app/services/api/api-client';
import { AppLoadingComponent } from 'src/app/share/components/loading/loading';
import { OrganizationDetailDto } from 'src/app/services/api/models/iammod/organization-detail-dto.model';
import { OrganizationUpdateDto } from 'src/app/services/api/models/iammod/organization-update-dto.model';
import { I18N_KEYS } from 'src/app/share/i18n-keys';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-edit',
  imports: [
    ...CommonModules,
    ...CommonFormModules,
    MatDialogModule,
  MatProgressSpinnerModule,
  AppLoadingComponent
  ],
  templateUrl: './edit.html',
  styleUrls: ['./edit.scss']
})
export class OrganizationEditComponent implements OnInit {
  readonly i18n = I18N_KEYS;
  orgForm!: FormGroup;
  isSubmitting = false;
  isLoading = signal(true);
  organization?: OrganizationDetailDto;

  constructor(
    private fb: FormBuilder,
    private api: ApiClient,
    private dialogRef: MatDialogRef<OrganizationEditComponent>,
    private snackBar: MatSnackBar,
    private translate: TranslateService,
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

  get name() {
    return this.orgForm.get('name') as FormControl;
  }

  get description() {
    return this.orgForm.get('description') as FormControl;
  }

  get displayOrder() {
    return this.orgForm.get('displayOrder') as FormControl;
  }

  loadOrganization(): void {
    this.api.organizations.getDetail(this.data.organizationId).subscribe({
      next: (org) => {
        this.organization = org;
        this.orgForm.patchValue({
          name: org.name,
          description: org.description || '',
          displayOrder: org.displayOrder
        });
  this.isLoading.set(false);
      },
      error: () => {
        this.snackBar.open(this.translate.instant(this.i18n.organization.loadFailed), this.translate.instant(this.i18n.common.close), { duration: 3000 });
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

    this.api.organizations.updateOrganization(this.data.organizationId, dto).subscribe({
      next: () => {
        this.snackBar.open(this.translate.instant(this.i18n.organization.updatedSuccess), this.translate.instant(this.i18n.common.close), { duration: 3000 });
        this.dialogRef.close(true);
      },
      error: (error) => {
        this.isSubmitting = false;
        const errorMsg = error?.error?.message || this.translate.instant(this.i18n.organization.updateFailed);
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
    if (control?.hasError('minlength')) {
      const minLength = control.errors?.['minlength'].requiredLength;
      return this.translate.instant(this.i18n.validation.minlength, { requiredLength: minLength });
    }
    if (control?.hasError('min')) {
      return this.translate.instant(this.i18n.organization.minValue);
    }
    return '';
  }
}
