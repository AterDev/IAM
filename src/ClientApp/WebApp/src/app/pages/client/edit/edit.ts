import { Component, OnInit, Inject, signal } from '@angular/core';
import { CommonModules, CommonFormModules } from 'src/app/share/shared-modules';
import { FormBuilder, FormGroup, FormControl, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { ApiClient } from 'src/app/services/api/api-client';
import { TranslateService } from '@ngx-translate/core';
import { AppLoadingComponent } from 'src/app/share/components/loading/loading';
import { COMMA, ENTER } from '@angular/cdk/keycodes';
import { MatChipInputEvent } from '@angular/material/chips';
import { ClientDetailDto } from 'src/app/services/api/models/iammod/client-detail-dto.model';
import { ClientUpdateDto } from 'src/app/services/api/models/iammod/client-update-dto.model';
import { ResourceItemDto } from 'src/app/services/api/models/iammod/resource-item-dto.model';
import { ScopeItemDto } from 'src/app/services/api/models/iammod/scope-item-dto.model';
import { ClientDetailViewModel, ClientUpdatePayload } from '../client-password-grant-policy.model';
import { I18N_KEYS } from 'src/app/share/i18n-keys';

@Component({
  selector: 'app-edit',
  imports: [
    ...CommonModules,
    ...CommonFormModules,
    MatDialogModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatIconModule,
    AppLoadingComponent
  ],
  templateUrl: './edit.html',
  styleUrls: ['./edit.scss']
})
export class ClientEditComponent implements OnInit {
  readonly i18n = I18N_KEYS;
  clientForm!: FormGroup;
  isSubmitting = false;
  isLoading = signal(true);
  client?: ClientDetailViewModel;
  availableResources = signal<ResourceItemDto[]>([]);
  availableScopes = signal<ScopeItemDto[]>([]);
  separatorKeysCodes = [ENTER, COMMA];

  redirectUris = signal<string[]>([]);
  postLogoutRedirectUris = signal<string[]>([]);
  scopeIds = signal<string[]>([]);
  resourceIds = signal<string[]>([]);

  constructor(
    private fb: FormBuilder,
    private api: ApiClient,
    private dialogRef: MatDialogRef<ClientEditComponent>,
    private snackBar: MatSnackBar,
    private translate: TranslateService,
    @Inject(MAT_DIALOG_DATA) public data: { clientId: string }
  ) { }

  ngOnInit(): void {
    this.clientForm = this.fb.group({
      displayName: ['', [Validators.required]],
      description: [''],
      consentType: [''],
      requirePkce: [true],
      allowPasswordGrant: [false],
      passwordGrantRestrictionReason: ['', [Validators.maxLength(500)]],
      newRedirectUri: [''],
      newPostLogoutRedirectUri: [''],
      newScope: ['']
    });

    this.allowPasswordGrantControl.valueChanges.subscribe(() => this.syncPasswordGrantRestrictionReasonState());

    this.loadClient();
  }

  loadClient(): void {
    this.api.clients.getDetail(this.data.clientId).subscribe({
      next: (client) => {
        this.client = client as ClientDetailViewModel;
        this.redirectUris.set(client.redirectUris || []);

        this.postLogoutRedirectUris.set(client.postLogoutRedirectUris || []);

        // Extract scope IDs (from ScopeItemDto objects)
        const scopeIds = (client.scopes || []).map(s => s.id).filter(id => !!id);
        console.log('[ClientEdit] Extracted scope IDs:', scopeIds);
        this.scopeIds.set(scopeIds);
        console.log('[ClientEdit] Scope IDs signal set, current value:', this.scopeIds());

        // 提取 resource IDs (从 ResourceItemDto 中提取 id)
        const resIds = (client.resources || []).map(r => r.id).filter(id => !!id);
        console.log('[ClientEdit] Extracted resource IDs:', resIds);

        this.resourceIds.set(resIds);
        console.log('[ClientEdit] Resource IDs signal set, current value:', this.resourceIds());

        this.clientForm.patchValue({
          displayName: client.displayName,
          description: client.description || '',
          consentType: client.consentType || '',
          requirePkce: client.requirePkce,
          allowPasswordGrant: this.client.allowPasswordGrant ?? false,
          passwordGrantRestrictionReason: this.client.passwordGrantRestrictionReason || ''
        });

        this.syncPasswordGrantRestrictionReasonState();

        this.loadAvailableScopes();
        this.loadAvailableResources();
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('[ClientEdit] Failed to load client:', error);
        this.snackBar.open(
          this.translate.instant('error.loadClientFailed'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
        this.dialogRef.close(false);
      }
    });
  }

  loadAvailableResources(): void {
    this.api.resources.getAll().subscribe({
      next: (resources) => {
        console.log('[ClientEdit] Available resources loaded:', resources);
        this.availableResources.set(resources || []);
      },
      error: (error) => {
        console.error('[ClientEdit] Failed to load resources:', error);
      }
    });
  }

  loadAvailableScopes(): void {
    this.api.scopes.getScopes(null, null, null, 1, 100, null).subscribe({
      next: (response) => {
        console.log('[ClientEdit] Available scopes loaded:', response);
        this.availableScopes.set(response.data || []);
      },
      error: (error) => {
        console.error('[ClientEdit] Failed to load scopes:', error);
      }
    });
  }

  addRedirectUri(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();
    if (value && !this.redirectUris().includes(value)) {
      this.redirectUris.update(uris => [...uris, value]);
    }
    event.chipInput?.clear();
    this.clientForm.get('newRedirectUri')?.setValue('');
  }

  removeRedirectUri(index: number): void {
    this.redirectUris.update(uris => uris.filter((_, i) => i !== index));
  }

  addPostLogoutRedirectUri(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();
    if (value && !this.postLogoutRedirectUris().includes(value)) {
      this.postLogoutRedirectUris.update(uris => [...uris, value]);
    }
    event.chipInput?.clear();
    this.clientForm.get('newPostLogoutRedirectUri')?.setValue('');
  }

  removePostLogoutRedirectUri(index: number): void {
    this.postLogoutRedirectUris.update(uris => uris.filter((_, i) => i !== index));
  }

  addScope(scope: ScopeItemDto): void {
    if (!this.scopeIds().includes(scope.id)) {
      this.scopeIds.update(ids => [...ids, scope.id]);
    }
  }

  removeScope(index: number): void {
    this.scopeIds.update(ids => ids.filter((_, i) => i !== index));
  }

  getScopeName(scopeId: string): string {
    return this.availableScopes().find(s => s.id === scopeId)?.displayName || scopeId;
  }

  addResource(resource: ResourceItemDto): void {
    if (!this.resourceIds().includes(resource.id)) {
      this.resourceIds.update(ids => [...ids, resource.id]);
    }
  }

  removeResource(index: number): void {
    this.resourceIds.update(ids => ids.filter((_, i) => i !== index));
  }

  getResourceName(resourceId: string): string {
    return this.availableResources().find(r => r.id === resourceId)?.displayName || resourceId;
  }

  onSubmit(): void {
    if (this.clientForm.invalid) {
      return;
    }

    this.isSubmitting = true;
    const formValue = this.clientForm.value;
    const dto: ClientUpdatePayload = {
      displayName: formValue.displayName,
      description: formValue.description || null,
      consentType: formValue.consentType || null,
      requirePkce: formValue.requirePkce,
      allowPasswordGrant: formValue.allowPasswordGrant,
      passwordGrantRestrictionReason: formValue.allowPasswordGrant
        ? null
        : (formValue.passwordGrantRestrictionReason?.trim() || null),
      redirectUris: this.redirectUris(),
      postLogoutRedirectUris: this.postLogoutRedirectUris(),
      scopeIds: this.scopeIds(),
      resourceIds: this.resourceIds()
    };

    this.api.clients.updateClient(this.data.clientId, dto as ClientUpdateDto).subscribe({
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

  syncPasswordGrantRestrictionReasonState(): void {
    if (this.allowPasswordGrantControl.value) {
      this.passwordGrantRestrictionReasonControl.setValue('');
      this.passwordGrantRestrictionReasonControl.disable({ emitEvent: false });
      return;
    }

    this.passwordGrantRestrictionReasonControl.enable({ emitEvent: false });
  }

  getErrorMessage(control: FormControl | null): string {
    if (!control) {
      return '';
    }
    if (control.hasError('required')) {
      return this.translate.instant('error.required');
    }
    if (control.hasError('maxlength')) {
      const maxLength = control.errors?.['maxlength'].requiredLength;
      return this.translate.instant('error.maxLength', { length: maxLength });
    }
    return '';
  }

  get displayNameControl() {
    return this.clientForm.get('displayName') as FormControl;
  }

  get allowPasswordGrantControl() {
    return this.clientForm.get('allowPasswordGrant') as FormControl;
  }

  get passwordGrantRestrictionReasonControl() {
    return this.clientForm.get('passwordGrantRestrictionReason') as FormControl;
  }
}
