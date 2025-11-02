import { Component, OnInit, Inject, signal } from '@angular/core';
import { CommonModules, CommonFormModules } from 'src/app/share/shared-modules';
import { FormBuilder, FormGroup, FormControl, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiClient } from 'src/app/services/api/api-client';
import { ResourceUpdateDto } from 'src/app/services/api/models/access-mod/resource-update-dto.model';
import { ResourceDetailDto } from 'src/app/services/api/models/access-mod/resource-detail-dto.model';
import { TranslateService } from '@ngx-translate/core';
import { AppLoadingComponent } from 'src/app/share/components/loading/loading';

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
export class ResourceEditComponent implements OnInit {
  resourceForm!: FormGroup;
  isSubmitting = false;
  isLoading = signal(true);
  resource?: ResourceDetailDto;

  constructor(
    private fb: FormBuilder,
    private api: ApiClient,
    private dialogRef: MatDialogRef<ResourceEditComponent>,
    private snackBar: MatSnackBar,
    private translate: TranslateService,
    @Inject(MAT_DIALOG_DATA) public data: { resourceId: string }
  ) {}

  ngOnInit(): void {
    this.resourceForm = this.fb.group({
      displayName: ['', [Validators.required]],
      description: ['']
    });

    this.loadResource();
  }

  loadResource(): void {
    this.api.resources.getDetail(this.data.resourceId).subscribe({
      next: (resource) => {
        this.resource = resource;
        this.resourceForm.patchValue({
          displayName: resource.displayName,
          description: resource.description || ''
        });
  this.isLoading.set(false);
      },
      error: () => {
        this.snackBar.open(
          this.translate.instant('error.loadResourceFailed'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
        this.dialogRef.close(false);
      }
    });
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
    const dto: ResourceUpdateDto = {
      displayName: formValue.displayName,
      description: formValue.description || null
    };

    this.api.resources.updateResource(this.data.resourceId, dto).subscribe({
      next: () => {
        this.snackBar.open(
          this.translate.instant('resource.updateSuccess'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
        this.dialogRef.close(true);
      },
      error: (error) => {
        this.isSubmitting = false;
        const errorMsg = error?.error?.message || this.translate.instant('error.updateResourceFailed');
        this.snackBar.open(errorMsg, this.translate.instant('common.close'), { duration: 3000 });
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  get displayName() {
    return this.resourceForm.get('displayName') as FormControl;
  }

  get description() {
    return this.resourceForm.get('description') as FormControl;
  }

  getErrorMessage(control: FormControl | null): string {
    if (!control) {
      return '';
    }
    if (control.hasError('required')) {
      return this.translate.instant('error.required');
    }
    return '';
  }
}
