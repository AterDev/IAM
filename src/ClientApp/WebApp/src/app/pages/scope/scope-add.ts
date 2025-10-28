import { Component, OnInit } from '@angular/core';
import { CommonModules, CommonFormModules } from 'src/app/share/shared-modules';
import { FormBuilder, FormGroup, FormControl, Validators, FormArray } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { ApiClient } from 'src/app/services/api/api-client';
import { ScopeAddDto } from 'src/app/services/api/models/access-mod/scope-add-dto.model';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-scope-add',
  imports: [
    ...CommonModules,
    ...CommonFormModules,
    MatDialogModule,
    MatChipsModule,
    MatIconModule,
    MatCheckboxModule
  ],
  templateUrl: './scope-add.html',
  styleUrls: ['./scope-add.scss']
})
export class ScopeAddComponent implements OnInit {
  scopeForm!: FormGroup;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private api: ApiClient,
    private dialogRef: MatDialogRef<ScopeAddComponent>,
    private snackBar: MatSnackBar,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.scopeForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      displayName: ['', [Validators.required]],
      description: [''],
      required: [false],
      emphasize: [false],
      claims: this.fb.array([]),
      newClaim: ['']
    });
  }

  get claims(): FormArray {
    return this.scopeForm.get('claims') as FormArray;
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
    const dto: ScopeAddDto = {
      name: formValue.name,
      displayName: formValue.displayName,
      description: formValue.description || null,
      required: formValue.required,
      emphasize: formValue.emphasize,
      claims: this.claims.value
    };

    this.api.scopes.createScope(dto).subscribe({
      next: () => {
        this.snackBar.open(
          this.translate.instant('scope.createSuccess'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
        this.dialogRef.close(true);
      },
      error: (error) => {
        this.isSubmitting = false;
        const errorMsg = error?.error?.message || this.translate.instant('error.createScopeFailed');
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

  get nameControl() {
    return this.scopeForm.get('name') as FormControl;
  }

  get displayNameControl() {
    return this.scopeForm.get('displayName') as FormControl;
  }
}
