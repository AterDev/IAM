import { Component, OnInit, Inject, signal } from '@angular/core';
import { CommonModules, CommonFormModules } from 'src/app/share/shared-modules';
import { FormBuilder, FormGroup, FormControl, Validators, FormArray } from '@angular/forms';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { ApiClient } from 'src/app/services/api/api-client';
import { ScopeUpdateDto } from 'src/app/services/api/models/access-mod/scope-update-dto.model';
import { ScopeDetailDto } from 'src/app/services/api/models/access-mod/scope-detail-dto.model';
import { TranslateService } from '@ngx-translate/core';
import { AppLoadingComponent } from 'src/app/share/components/loading/loading';

@Component({
  selector: 'app-edit',
  imports: [
    ...CommonModules,
    ...CommonFormModules,
    MatDialogModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatIconModule,
  MatCheckboxModule,
  AppLoadingComponent
  ],
  templateUrl: './edit.html',
  styleUrls: ['./edit.scss']
})
export class ScopeEditComponent implements OnInit {
  scopeForm!: FormGroup;
  isSubmitting = false;
  isLoading = signal(true);
  scope?: ScopeDetailDto;

  constructor(
    private fb: FormBuilder,
    private api: ApiClient,
    private dialogRef: MatDialogRef<ScopeEditComponent>,
    private snackBar: MatSnackBar,
    private translate: TranslateService,
    @Inject(MAT_DIALOG_DATA) public data: { scopeId: string }
  ) {}

  ngOnInit(): void {
    this.scopeForm = this.fb.group({
      displayName: ['', [Validators.required]],
      description: [''],
      required: [false],
      emphasize: [false],
      claims: this.fb.array([]),
      newClaim: ['']
    });

    this.loadScope();
  }

  get claims(): FormArray {
    return this.scopeForm.get('claims') as FormArray;
  }

  loadScope(): void {
    this.api.scopes.getDetail(this.data.scopeId).subscribe({
      next: (scope) => {
        this.scope = scope;

        scope.claims.forEach(claim => {
          this.claims.push(this.fb.control(claim));
        });

        this.scopeForm.patchValue({
          displayName: scope.displayName,
          description: scope.description || '',
          required: scope.required,
          emphasize: scope.emphasize
        });

  this.isLoading.set(false);
      },
      error: () => {
        this.snackBar.open(
          this.translate.instant('error.loadScopeFailed'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
        this.dialogRef.close(false);
      }
    });
  }

  addClaim(): void {
    const claimControl = this.scopeForm.get('newClaim');
    const claim = claimControl?.value?.trim();
    if (claim) {
      this.claims.push(this.fb.control(claim));
      claimControl?.setValue('');
    }
  }

  removeClaim(index: number): void {
    this.claims.removeAt(index);
  }

  onSubmit(): void {
    if (this.scopeForm.invalid) {
      Object.keys(this.scopeForm.controls).forEach(key => {
        this.scopeForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.isSubmitting = true;
    const formValue = this.scopeForm.value;
    const dto: ScopeUpdateDto = {
      displayName: formValue.displayName,
      description: formValue.description || null,
      required: formValue.required,
      emphasize: formValue.emphasize,
      claims: this.claims.value
    };

    this.api.scopes.updateScope(this.data.scopeId, dto).subscribe({
      next: () => {
        this.snackBar.open(
          this.translate.instant('scope.updateSuccess'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
        this.dialogRef.close(true);
      },
      error: (error) => {
        this.isSubmitting = false;
        const errorMsg = error?.error?.message || this.translate.instant('error.updateScopeFailed');
        this.snackBar.open(errorMsg, this.translate.instant('common.close'), { duration: 3000 });
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close(false);
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

  get displayNameControl() {
    return this.scopeForm.get('displayName') as FormControl;
  }
}
