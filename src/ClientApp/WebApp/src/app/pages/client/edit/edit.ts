import { Component, OnInit, Inject, signal } from '@angular/core';
import { CommonModules, CommonFormModules } from 'src/app/share/shared-modules';
import { FormBuilder, FormGroup, FormControl, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { ApiClient } from 'src/app/services/api/api-client';
import { ClientUpdateDto } from 'src/app/services/api/models/access-mod/client-update-dto.model';
import { ClientDetailDto } from 'src/app/services/api/models/access-mod/client-detail-dto.model';
import { ResourceItemDto } from 'src/app/services/api/models/access-mod/resource-item-dto.model';
import { TranslateService } from '@ngx-translate/core';
import { AppLoadingComponent } from 'src/app/share/components/loading/loading';
import { COMMA, ENTER } from '@angular/cdk/keycodes';
import { MatChipInputEvent } from '@angular/material/chips';

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
  clientForm!: FormGroup;
  isSubmitting = false;
  isLoading = signal(true);
  client?: ClientDetailDto;
  availableResources = signal<ResourceItemDto[]>([]);
  separatorKeysCodes = [ENTER, COMMA];
  
  redirectUris = signal<string[]>([]);
  postLogoutRedirectUris = signal<string[]>([]);
  scopes = signal<string[]>([]);
  resourceIds = signal<string[]>([]);

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
      newRedirectUri: [''],
      newPostLogoutRedirectUri: [''],
      newScope: ['']
    });

    this.loadClient();
  }

  loadClient(): void {
    console.log('[ClientEdit] Loading client:', this.data.clientId);
    this.api.clients.getDetail(this.data.clientId).subscribe({
      next: (client) => {
        console.log('[ClientEdit] Client loaded:', client);
        console.log('[ClientEdit] Client scopes raw:', client.scopes);
        console.log('[ClientEdit] Client resources raw:', client.resources);
        this.client = client;

        this.redirectUris.set(client.redirectUris || []);
        console.log('[ClientEdit] Redirect URIs set:', this.redirectUris());
        
        this.postLogoutRedirectUris.set(client.postLogoutRedirectUris || []);
        console.log('[ClientEdit] Post logout URIs set:', this.postLogoutRedirectUris());
        
        // 从 ScopeItemDto 数组中提取 scope names
        const scopeNames = (client.scopes || []).map(s => {
          console.log('[ClientEdit] Processing scope:', s);
          return (s as any).name || (typeof s === 'string' ? s : '');
        }).filter(name => !!name);
        console.log('[ClientEdit] Extracted scope names:', scopeNames);
        
        this.scopes.set(scopeNames);
        console.log('[ClientEdit] Scopes signal set, current value:', this.scopes());

        // 提取 resource IDs (从 ResourceItemDto 中提取 id)
        const resIds = (client.resources || []).map(r => r.id).filter(id => !!id);
        console.log('[ClientEdit] Extracted resource IDs:', resIds);
        
        this.resourceIds.set(resIds);
        console.log('[ClientEdit] Resource IDs signal set, current value:', this.resourceIds());

        this.clientForm.patchValue({
          displayName: client.displayName,
          description: client.description || '',
          consentType: client.consentType || '',
          requirePkce: client.requirePkce
        });

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
    this.api.resources.getList().subscribe({
      next: (resources) => {
        console.log('[ClientEdit] Available resources loaded:', resources);
        this.availableResources.set(resources || []);
      },
      error: (error) => {
        console.error('[ClientEdit] Failed to load resources:', error);
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

  addScope(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();
    if (value && !this.scopes().includes(value)) {
      this.scopes.update(scopes => [...scopes, value]);
    }
    event.chipInput?.clear();
    this.clientForm.get('newScope')?.setValue('');
  }

  removeScope(index: number): void {
    this.scopes.update(scopes => scopes.filter((_, i) => i !== index));
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
      redirectUris: this.redirectUris(),
      postLogoutRedirectUris: this.postLogoutRedirectUris(),
      scopeIds: this.scopes(),
      resourceIds: this.resourceIds()
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

  get displayNameControl() {
    return this.clientForm.get('displayName') as FormControl;
  }
}
