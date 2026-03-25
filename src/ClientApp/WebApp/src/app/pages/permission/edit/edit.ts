import { Component, Inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { DotnetSwaggerClient } from 'src/app/services/dotnet-swagger/dotnet-swagger-client';
import { PermissionType } from 'src/app/services/dotnet-swagger/models/entity/permission-type.model';
import { ClientItemDto } from 'src/app/services/dotnet-swagger/models/iammod/client-item-dto.model';
import { PermissionDetailDto } from 'src/app/services/dotnet-swagger/models/iammod/permission-detail-dto.model';
import { PermissionUpsertDto } from 'src/app/services/dotnet-swagger/models/iammod/permission-upsert-dto.model';
import { CommonModules, BaseMatModules, CommonFormModules } from 'src/app/share/shared-modules';
import { I18N_KEYS } from 'src/app/share/i18n-keys';

export interface PermissionParentOption {
  id: string;
  name: string;
}

export interface PermissionEditDialogData {
  permission?: PermissionDetailDto | null;
  parentOptions: PermissionParentOption[];
  defaultParentId?: string | null;
  currentClientId?: string | null;
  currentClientLabel?: string | null;
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
    name: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(200)] }),
    description: new FormControl<string | null>(null),
    type: new FormControl(PermissionType.Menu, { nonNullable: true, validators: [Validators.required] }),
    parentId: new FormControl<string | null>(null),
    path: new FormControl<string | null>(null),
    ownedClientId: new FormControl<string | null>(null),
  });

  constructor(
    private readonly api: DotnetSwaggerClient,
    private readonly dialogRef: MatDialogRef<PermissionEditComponent>,
    private readonly snackBar: MatSnackBar,
    private readonly translate: TranslateService,
    @Inject(MAT_DIALOG_DATA) readonly data: PermissionEditDialogData,
  ) {}

  ngOnInit(): void {
    const permission = this.data.permission;
    if (permission) {
      this.form.patchValue({
        code: permission.code,
        name: permission.name,
        description: permission.description ?? null,
        type: permission.type,
        parentId: permission.parentId ?? null,
        path: permission.path ?? null,
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
  get nameField() { return this.form.controls.name; }
  get type() { return this.form.controls.type; }
  get parentId() { return this.form.controls.parentId; }
  get path() { return this.form.controls.path; }
  get currentClientLabel() { return this.data.currentClientLabel; }

  save(): void {
    if (this.form.invalid || this.isSaving()) {
      this.form.markAllAsTouched();
      return;
    }

    const payload: PermissionUpsertDto = {
      code: this.code.value.trim(),
      name: this.nameField.value.trim(),
      description: this.form.controls.description.value,
      type: this.type.value,
      parentId: this.parentId.value,
      path: this.path.value,
      ownedClientId: this.data.currentClientId ?? this.form.controls.ownedClientId.value,
    };

    this.isSaving.set(true);
    const request$ = this.data.permission
      ? this.api.permissions.update(this.data.permission.id, payload)
      : this.api.permissions.create(payload);

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