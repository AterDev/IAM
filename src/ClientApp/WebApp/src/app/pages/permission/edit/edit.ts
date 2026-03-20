import { Component, Inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { CommonModules, BaseMatModules, CommonFormModules } from 'src/app/share/shared-modules';
import { PermissionAdminService } from 'src/app/services/permission-admin.service';
import { PermissionItem, PermissionType, PermissionUpsertDto } from 'src/app/services/permission-admin.models';
import { ClientItemDto } from 'src/app/services/api/models/iammod/client-item-dto.model';

export interface PermissionEditDialogData {
  permission?: PermissionItem | null;
  parentOptions: PermissionItem[];
  clients: ClientItemDto[];
  defaultParentId?: string | null;
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
  protected readonly permissionTypes = [
    { labelKey: 'permission.typeOptions.menu', value: PermissionType.Menu },
    { labelKey: 'permission.typeOptions.button', value: PermissionType.Button },
    { labelKey: 'permission.typeOptions.business', value: PermissionType.Business },
  ];

  readonly isSaving = signal(false);
  readonly form = new FormGroup({
    code: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(200)] }),
    name: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(200)] }),
    displayName: new FormControl<string | null>(null),
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
      this.form.patchValue({
        code: permission.code,
        name: permission.name,
        displayName: permission.displayName ?? null,
        description: permission.description ?? null,
        type: permission.type,
        parentId: permission.parentId ?? null,
        namespace: permission.namespace ?? null,
        resource: permission.resource ?? null,
        action: permission.action ?? null,
        path: permission.path ?? null,
        icon: permission.icon ?? null,
        sort: permission.sort,
        ownedClientId: permission.ownedClientId ?? null,
      });
      return;
    }

    if (this.data.defaultParentId) {
      this.form.patchValue({ parentId: this.data.defaultParentId });
    }
  }

  get code() { return this.form.controls.code; }
  get name() { return this.form.controls.name; }
  get type() { return this.form.controls.type; }
  get parentId() { return this.form.controls.parentId; }
  get namespaceField() { return this.form.controls.namespace; }
  get resource() { return this.form.controls.resource; }
  get action() { return this.form.controls.action; }
  get path() { return this.form.controls.path; }
  get icon() { return this.form.controls.icon; }
  get sort() { return this.form.controls.sort; }
  get ownedClientId() { return this.form.controls.ownedClientId; }

  save(): void {
    if (this.form.invalid || this.isSaving()) {
      this.form.markAllAsTouched();
      return;
    }

    const payload: PermissionUpsertDto = {
      code: this.code.value,
      name: this.name.value,
      displayName: this.form.controls.displayName.value,
      description: this.form.controls.description.value,
      type: this.type.value,
      parentId: this.parentId.value,
      namespace: this.namespaceField.value,
      resource: this.resource.value,
      action: this.action.value,
      path: this.path.value,
      icon: this.icon.value,
      sort: this.sort.value,
      ownedClientId: this.ownedClientId.value,
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