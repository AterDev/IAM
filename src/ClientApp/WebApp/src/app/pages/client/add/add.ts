import { Component, OnInit } from '@angular/core';
import { CommonModules, CommonFormModules } from 'src/app/share/shared-modules';
import { FormBuilder, FormGroup, FormControl, Validators, FormArray } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { ApiClient } from 'src/app/services/api/api-client';
import { ClientAddDto } from 'src/app/services/api/models/access-mod/client-add-dto.model';
import { TranslateService } from '@ngx-translate/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Clipboard } from '@angular/cdk/clipboard';

@Component({
  selector: 'app-add',
  imports: [
    ...CommonModules,
    ...CommonFormModules,
    MatDialogModule,
    MatChipsModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './add.html',
  styleUrls: ['./add.scss']
})
export class ClientAddComponent implements OnInit {
  clientForm!: FormGroup;
  isSubmitting = false;
  clientSecret: string | null = null;
  secretCopied = false;

  constructor(
    private fb: FormBuilder,
    private api: ApiClient,
    private dialogRef: MatDialogRef<ClientAddComponent>,
    private snackBar: MatSnackBar,
    private translate: TranslateService,
    private clipboard: Clipboard
  ) {}

  ngOnInit(): void {
    this.clientForm = this.fb.group({
      clientId: ['', [Validators.required, Validators.minLength(3)]],
      displayName: ['', [Validators.required]],
      description: [''],
      type: ['confidential', [Validators.required]],
      requirePkce: [true],
      consentType: ['explicit'],
      applicationType: ['web'],
      redirectUris: this.fb.array([]),
      postLogoutRedirectUris: this.fb.array([]),
      newRedirectUri: [''],
      newPostLogoutRedirectUri: ['']
    });
  }

  get clientId() {
    return this.clientForm.get('clientId') as FormControl;
  }

  get displayName() {
    return this.clientForm.get('displayName') as FormControl;
  }

  get description() {
    return this.clientForm.get('description') as FormControl;
  }

  get type() {
    return this.clientForm.get('type') as FormControl;
  }

  get applicationType() {
    return this.clientForm.get('applicationType') as FormControl;
  }

  get consentType() {
    return this.clientForm.get('consentType') as FormControl;
  }

  get requirePkce() {
    return this.clientForm.get('requirePkce') as FormControl;
  }

  get newRedirectUri() {
    return this.clientForm.get('newRedirectUri') as FormControl;
  }

  get newPostLogoutRedirectUri() {
    return this.clientForm.get('newPostLogoutRedirectUri') as FormControl;
  }

  get redirectUris(): FormArray {
    return this.clientForm.get('redirectUris') as FormArray;
  }

  get postLogoutRedirectUris(): FormArray {
    return this.clientForm.get('postLogoutRedirectUris') as FormArray;
  }

  addRedirectUri(): void {
    const uri = this.newRedirectUri.value?.trim();
    if (uri) {
      this.redirectUris.push(this.fb.control(uri));
      this.newRedirectUri.setValue('');
    }
  }

  removeRedirectUri(index: number): void {
    this.redirectUris.removeAt(index);
  }

  addPostLogoutRedirectUri(): void {
    const uri = this.newPostLogoutRedirectUri.value?.trim();
    if (uri) {
      this.postLogoutRedirectUris.push(this.fb.control(uri));
      this.newPostLogoutRedirectUri.setValue('');
    }
  }

  removePostLogoutRedirectUri(index: number): void {
    this.postLogoutRedirectUris.removeAt(index);
  }

  copySecret(): void {
    if (this.clientSecret) {
      this.clipboard.copy(this.clientSecret);
      this.secretCopied = true;
      this.snackBar.open(
        this.translate.instant('client.secretCopied'),
        this.translate.instant('common.close'),
        { duration: 2000 }
      );
    }
  }

  onSubmit(): void {
    if (this.clientForm.invalid) {
      Object.keys(this.clientForm.controls).forEach(key => {
        this.clientForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.isSubmitting = true;
    const formValue = this.clientForm.value;
    const dto: ClientAddDto = {
      clientId: formValue.clientId,
      displayName: formValue.displayName,
      description: formValue.description || null,
      type: formValue.type || null,
      requirePkce: formValue.requirePkce,
      consentType: formValue.consentType || null,
      applicationType: formValue.applicationType || null,
      redirectUris: this.redirectUris.value,
      postLogoutRedirectUris: this.postLogoutRedirectUris.value,
      scopeIds: []
    };

    this.api.clients.createClient(dto).subscribe({
      next: (response) => {
        this.clientSecret = response.secret;
        this.snackBar.open(
          this.translate.instant('client.createSuccess'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
      },
      error: (error) => {
        this.isSubmitting = false;
        const errorMsg = error?.error?.message || this.translate.instant('error.createClientFailed');
        this.snackBar.open(errorMsg, this.translate.instant('common.close'), { duration: 3000 });
      }
    });
  }

  onClose(): void {
    if (this.clientSecret && !this.secretCopied) {
      const confirmed = confirm(this.translate.instant('client.secretNotCopiedWarning'));
      if (!confirmed) {
        return;
      }
    }
    this.dialogRef.close(this.clientSecret ? true : false);
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
