import { Component, Inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { CommonModules, BaseMatModules, CommonFormModules } from 'src/app/share/shared-modules';
import { PermissionAdminService } from 'src/app/services/permission-admin.service';
import { PermissionItem, PermissionType, PermissionUpsertDto } from 'src/app/services/permission-admin.models';
import { ClientItemDto } from 'src/app/services/api/models/iammod/client-item-dto.model';
import { I18N_KEYS } from 'src/app/share/i18n-keys';

export interface PermissionEditDialogData {
  permission?: PermissionItem | null;
  parentOptions: PermissionItem[];
  clients: ClientItemDto[];
  defaultParentId?: string | null;
  currentClientId?: string | null;
  currentClientLabel?: string | null;
  lockClient?: boolean;
}

@Component({
  selector: 'app-permission-edit',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    ...CommonFormModules,
    MatDialogModule,
    ReactiveFormsModule,
  ],
  templateUrl: './edit.html',
  styleUrls: ['./edit.scss'],
})
export class PermissionEditComponent implements OnInit {
  readonly i18n = I18N_KEYS;
  protected readonly permissionTypes = [
    { labelKey: 'permission.typeOptions.menu', value: PermissionType.Menu },
    { labelKey: 'permission.typeOptions.button', value: PermissionType.Button },
    { labelKey: 'permission.typeOptions.business', value: PermissionType.Business },
  ];

  readonly isSaving = signal(false);
  readonly form = new FormGroup({
    code: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(200)] }),
    name: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(200)] }),
    displayName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(200)] }),
    description: new FormControl<string | null>(null),
    type: new FormControl(PermissionType.Menu, { nonNullable: true, validators: [Validators.required] }),
    parentId: new FormControl<string | null>(null),
    namespace: new FormControl<string | null>(null),
    resource: new FormControl<string | null>(null),
    action: new FormControl<string | null>(null),
    path: new FormControl<string | null>(null),
    icon: new FormControl<string | null>(null),
    sort: new FormControl(0, { nonNullable: true }),
    ownedClientId: new FormControl<string | null>(null),
  });

  constructor(
    private readonly permissionAdminService: PermissionAdminService,
    private readonly dialogRef: MatDialogRef<PermissionEditComponent>,
    private readonly snackBar: MatSnackBar,
    private readonly translate: TranslateService,
    @Inject(MAT_DIALOG_DATA) readonly data: PermissionEditDialogData,
  ) {}

  ngOnInit(): void {
    const permission = this.data.permission;
    if (permission) {
      const displayLabel = permission.displayName || permission.name;
      this.form.patchValue({
        code: permission.code,
        name: displayLabel,
        displayName: displayLabel,
        description: permission.description ?? null,
        type: permission.type,
        parentId: permission.parentId ?? null,
        namespace: permission.namespace ?? null,
        resource: permission.resource ?? null,
        action: permission.action ?? null,
        path: permission.path ?? null,
        icon: permission.icon ?? null,
        sort: permission.sort,
        ownedClientId: permission.ownedClientId ?? this.data.currentClientId ?? null,
      });
      return;
    }

    this.form.patchValue({ ownedClientId: this.data.currentClientId ?? null });

    if (this.data.defaultParentId) {
      this.form.patchValue({ parentId: this.data.defaultParentId });
    }
  }

  get code() { return this.form.controls.code; }
  get displayNameField() { return this.form.controls.displayName; }
  get type() { return this.form.controls.type; }
  get parentId() { return this.form.controls.parentId; }
  get path() { return this.form.controls.path; }
  get icon() { return this.form.controls.icon; }
  get sort() { return this.form.controls.sort; }
  get currentClientLabel() { return this.data.currentClientLabel; }

  save(): void {
    if (this.form.invalid || this.isSaving()) {
      this.form.markAllAsTouched();
      return;
    }

    const displayLabel = this.displayNameField.value.trim();

    const payload: PermissionUpsertDto = {
      code: this.code.value,
      name: displayLabel,
      displayName: displayLabel,
      description: this.form.controls.description.value,
      type: this.type.value,
      parentId: this.parentId.value,
      namespace: this.form.controls.namespace.value,
      resource: this.form.controls.resource.value,
      action: this.form.controls.action.value,
      path: this.path.value,
      icon: this.icon.value,
      sort: this.sort.value,
      ownedClientId: this.data.currentClientId ?? this.form.controls.ownedClientId.value,
    };

    this.isSaving.set(true);
    const request$ = this.data.permission
      ? this.permissionAdminService.updatePermission(this.data.permission.id, payload)
      : this.permissionAdminService.createPermission(payload);

    request$.subscribe({
      next: (result) => {
        this.snackBar.open(
          this.translate.instant('permission.saveSuccess'),
          this.translate.instant('common.close'),
          { duration: 3000 },
        );
        this.dialogRef.close(result);
      },
      error: () => {
        this.isSaving.set(false);
        this.snackBar.open(
          this.translate.instant('permission.saveFailed'),
          this.translate.instant('common.close'),
          { duration: 3000 },
        );
      },
    });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}