import { Component, OnInit, Inject } from '@angular/core';
import { CommonModules, CommonFormModules } from 'src/app/share/shared-modules';
import { FormBuilder, FormGroup, FormControl, Validators, FormArray } from '@angular/forms';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { ApiClient } from 'src/app/services/api/api-client';
import { ClientUpdateDto } from 'src/app/services/api/models/access-mod/client-update-dto.model';
import { ClientDetailDto } from 'src/app/services/api/models/access-mod/client-detail-dto.model';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-client-edit',
  imports: [
    ...CommonModules,
    ...CommonFormModules,
    MatDialogModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatIconModule
  ],
  templateUrl: './client-edit.html',
  styleUrls: ['./client-edit.scss']
})
export class ClientEditComponent implements OnInit {
  clientForm!: FormGroup;
  isSubmitting = false;
  isLoading = true;
  client?: ClientDetailDto;

  constructor(
    private fb: FormBuilder,
    private api: ApiClient,
    private dialogRef: MatDialogRef<ClientEditComponent>,
    private snackBar: MatSnackBar,
    private translate: TranslateService,
    @Inject(MAT_DIALOG_DATA) public data: { clientId: string }
  ) {}

  ngOnInit(): void {
    this.clientForm = this.fb.group({
      displayName: ['', [Validators.required]],
      description: [''],
      consentType: [''],
      requirePkce: [true],
      redirectUris: this.fb.array([]),
      postLogoutRedirectUris: this.fb.array([]),
      newRedirectUri: [''],
      newPostLogoutRedirectUri: ['']
    });

    this.loadClient();
  }

  get redirectUris(): FormArray {
    return this.clientForm.get('redirectUris') as FormArray;
  }

  get postLogoutRedirectUris(): FormArray {
    return this.clientForm.get('postLogoutRedirectUris') as FormArray;
  }

  loadClient(): void {
    this.api.clients.getDetail(this.data.clientId).subscribe({
      next: (client) => {
        this.client = client;
        
        client.redirectUris.forEach(uri => {
          this.redirectUris.push(this.fb.control(uri));
        });
        
        client.postLogoutRedirectUris.forEach(uri => {
          this.postLogoutRedirectUris.push(this.fb.control(uri));
        });

        this.clientForm.patchValue({
          displayName: client.displayName,
          description: client.description || '',
          consentType: client.consentType || '',
          requirePkce: client.requirePkce
        });
        
        this.isLoading = false;
      },
      error: () => {
        this.snackBar.open(
          this.translate.instant('error.loadClientFailed'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
        this.dialogRef.close(false);
      }
    });
  }

  addRedirectUri(): void {
    const uriControl = this.clientForm.get('newRedirectUri');
    const uri = uriControl?.value?.trim();
    if (uri) {
      this.redirectUris.push(this.fb.control(uri));
      uriControl?.setValue('');
    }
  }

  removeRedirectUri(index: number): void {
    this.redirectUris.removeAt(index);
  }

  addPostLogoutRedirectUri(): void {
    const uriControl = this.clientForm.get('newPostLogoutRedirectUri');
    const uri = uriControl?.value?.trim();
    if (uri) {
      this.postLogoutRedirectUris.push(this.fb.control(uri));
      uriControl?.setValue('');
    }
  }

  removePostLogoutRedirectUri(index: number): void {
    this.postLogoutRedirectUris.removeAt(index);
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
    const dto: ClientUpdateDto = {
      displayName: formValue.displayName,
      description: formValue.description || null,
      consentType: formValue.consentType || null,
      requirePkce: formValue.requirePkce,
      redirectUris: this.redirectUris.value,
      postLogoutRedirectUris: this.postLogoutRedirectUris.value
    };

    this.api.clients.updateClient(this.data.clientId, dto).subscribe({
      next: () => {
        this.snackBar.open(
          this.translate.instant('client.updateSuccess'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
        this.dialogRef.close(true);
      },
      error: (error) => {
        this.isSubmitting = false;
        const errorMsg = error?.error?.message || this.translate.instant('error.updateClientFailed');
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
}
