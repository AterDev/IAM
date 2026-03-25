import { Component, Inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { CommonModules, BaseMatModules, CommonFormModules } from 'src/app/share/shared-modules';
import { PermissionType } from 'src/app/services/dotnet-swagger/models/entity/permission-type.model';
import { PermissionSyncNodeDto } from 'src/app/services/dotnet-swagger/models/iammod/permission-sync-node-dto.model';

export interface ClientPermissionNodeDialogData {
  node?: PermissionSyncNodeDto | null;
}

@Component({
  selector: 'app-client-permission-node-dialog',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    ...CommonFormModules,
    MatDialogModule,
    ReactiveFormsModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.node ? ('client.editPermissionNode' | translate) : ('client.addPermissionNode' | translate) }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="node-form">
        <div class="node-grid">
          <mat-form-field appearance="outline"><mat-label>{{ 'permission.code' | translate }}</mat-label><input matInput [formControl]="code" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>{{ 'permission.name' | translate }}</mat-label><input matInput [formControl]="name" /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>{{ 'permission.type' | translate }}</mat-label>
            <mat-select [formControl]="type">
              <mat-option [value]="permissionType.Menu">{{ 'permission.typeOptions.menu' | translate }}</mat-option>
              <mat-option [value]="permissionType.Button">{{ 'permission.typeOptions.button' | translate }}</mat-option>
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline"><mat-label>{{ 'permission.path' | translate }}</mat-label><input matInput [formControl]="path" /></mat-form-field>
        </div>
        <mat-form-field appearance="outline" class="full-width"><mat-label>{{ 'permission.description' | translate }}</mat-label><textarea matInput rows="4" [formControl]="form.controls.description"></textarea></mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="close()">{{ 'common.cancel' | translate }}</button>
      <button mat-raised-button (click)="save()">{{ 'common.save' | translate }}</button>
    </mat-dialog-actions>
  `,
  styles: [`.node-form{min-width:min(760px,80vw)}.node-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px}.full-width{width:100%;margin-top:16px}@media(max-width:768px){.node-form{min-width:auto}.node-grid{grid-template-columns:1fr;}}`],
})
export class ClientPermissionNodeDialogComponent {
  protected readonly permissionType = PermissionType;
  readonly form: FormGroup<{
    code: FormControl<string>;
    name: FormControl<string>;
    description: FormControl<string | null>;
    type: FormControl<PermissionType>;
    path: FormControl<string | null>;
  }>;

  constructor(
    private readonly dialogRef: MatDialogRef<ClientPermissionNodeDialogComponent>,
    @Inject(MAT_DIALOG_DATA) readonly data: ClientPermissionNodeDialogData,
  ) {
    const node = data.node;
    this.form = new FormGroup({
      code: new FormControl(node?.code ?? '', { nonNullable: true, validators: [Validators.required] }),
      name: new FormControl(node?.name ?? '', { nonNullable: true, validators: [Validators.required] }),
      description: new FormControl<string | null>(node?.description ?? null),
      type: new FormControl<PermissionType>(node?.type ?? PermissionType.Menu, { nonNullable: true }),
      path: new FormControl<string | null>(node?.path ?? null),
    });
  }

  get code() { return this.form.controls.code; }
  get name() { return this.form.controls.name; }
  get type() { return this.form.controls.type; }
  get path() { return this.form.controls.path; }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.dialogRef.close({
      code: this.code.value,
      name: this.name.value,
      description: this.form.controls.description.value,
      type: this.type.value,
      path: this.path.value,
      children: this.data.node?.children ?? [],
    } satisfies PermissionSyncNodeDto);
  }

  close(): void {
    this.dialogRef.close();
  }
}