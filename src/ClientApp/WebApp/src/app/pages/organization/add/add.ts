import { Component, OnInit, Inject } from '@angular/core';
import { CommonModules, CommonFormModules } from 'src/app/share/shared-modules';
import { FormBuilder, FormGroup, FormControl, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiClient } from 'src/app/services/api/api-client';
import { OrganizationAddDto } from 'src/app/services/api/models/iammod/organization-add-dto.model';
import { I18N_KEYS } from 'src/app/share/i18n-keys';
import { TranslateService } from '@ngx-translate/core';

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
export class OrganizationAddComponent implements OnInit {
  readonly i18n = I18N_KEYS;
  orgForm!: FormGroup;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private api: ApiClient,
    private dialogRef: MatDialogRef<OrganizationAddComponent>,
    private snackBar: MatSnackBar,
    private translate: TranslateService,
    @Inject(MAT_DIALOG_DATA) public data: { parentId: string | null }
  ) {}

  ngOnInit(): void {
    this.orgForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      description: [''],
      displayOrder: [0, [Validators.min(0)]]
    });
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

    this.api.organizations.createOrganization(dto).subscribe({
      next: () => {
        this.snackBar.open(this.translate.instant(this.i18n.organization.createdSuccess), this.translate.instant(this.i18n.common.close), { duration: 3000 });
        this.dialogRef.close(true);
      },
      error: (error) => {
        this.isSubmitting = false;
        const errorMsg = error?.error?.message || this.translate.instant(this.i18n.organization.createFailed);
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
