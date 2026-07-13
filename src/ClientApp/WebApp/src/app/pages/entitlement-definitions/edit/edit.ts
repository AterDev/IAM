import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiClient } from 'src/app/services/api/api-client';
import { UserEntitlementType } from 'src/app/services/api/models/entity/user-entitlement-type.model';
import { UserEntitlementDefinitionItemDto } from 'src/app/services/api/models/user-center-mod/user-entitlement-definition-item-dto.model';
import { BaseMatModules, CommonModules } from 'src/app/share/shared-modules';

@Component({ selector: 'app-entitlement-definition-edit', imports: [...CommonModules, ...BaseMatModules, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule], templateUrl: './edit.html' })
export class EntitlementDefinitionEditComponent {
  private readonly api = inject(ApiClient); private readonly dialog = inject(MatDialogRef<EntitlementDefinitionEditComponent>); private readonly snack = inject(MatSnackBar); private readonly fb = inject(FormBuilder);
  readonly data = inject<UserEntitlementDefinitionItemDto>(MAT_DIALOG_DATA);
  readonly type = UserEntitlementType;
  readonly form = this.fb.nonNullable.group({ displayName: [this.data.displayName, Validators.required], description: [this.data.description ?? ''], entitlementCode: [this.data.entitlementCode, Validators.required], entitlementType: [this.data.entitlementType], unit: [this.data.unit, Validators.required] });
  save(): void { if (this.form.invalid) return; this.api.userEntitlementDefinitions.update(this.data.id, this.form.getRawValue()).subscribe({ next: () => this.dialog.close(true), error: () => this.snack.open('Failed to save', 'Close', { duration: 3000 }) }); }
}
