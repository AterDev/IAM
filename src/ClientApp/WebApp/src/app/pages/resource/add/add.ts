import { Component, OnInit } from '@angular/core';
import { CommonModules, CommonFormModules } from 'src/app/share/shared-modules';
import { FormBuilder, FormGroup, FormControl, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiClient } from 'src/app/services/api/api-client';
import { ResourceAddDto } from 'src/app/services/api/models/iammod/resource-add-dto.model';
import { TranslateService } from '@ngx-translate/core';
import { I18N_KEYS } from 'src/app/share/i18n-keys';

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
export class ResourceAddComponent implements OnInit {
  readonly i18n = I18N_KEYS;
  resourceForm!: FormGroup;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private api: ApiClient,
    private dialogRef: MatDialogRef<ResourceAddComponent>,
    private snackBar: MatSnackBar,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.resourceForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      displayName: ['', [Validators.required]],
      description: ['']
    });
  }

  get name() {
    return this.resourceForm.get('name') as FormControl;
  }

  get displayName() {
    return this.resourceForm.get('displayName') as FormControl;
  }

  get description() {
    return this.resourceForm.get('description') as FormControl;
  }

  onSubmit(): void {
    if (this.resourceForm.invalid) {
      Object.keys(this.resourceForm.controls).forEach(key => {
        this.resourceForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.isSubmitting = true;
    const formValue = this.resourceForm.value;
    const dto: ResourceAddDto = {
      name: formValue.name,
      displayName: formValue.displayName,
      description: formValue.description || null
    };

    this.api.resources.createResource(dto).subscribe({
      next: () => {
        this.snackBar.open(
          this.translate.instant('resource.createSuccess'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
        this.dialogRef.close(true);
      },
      error: (error) => {
        this.isSubmitting = false;
        const errorMsg = error?.error?.message || this.translate.instant('error.createResourceFailed');
        this.snackBar.open(errorMsg, this.translate.instant('common.close'), { duration: 3000 });
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  getErrorMessage(control: FormControl | null, fieldName: string): string {
    if (!control) {
      return '';
    }
    if (control.hasError('required')) {
      return this.translate.instant('error.required');
    }
    if (control.hasError('minlength')) {
      const minLength = control.errors?.['minlength'].requiredLength;
      return this.translate.instant('error.minLength', { length: minLength });
    }
    return '';
  }
}
