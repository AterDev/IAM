import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ApiClient } from 'src/app/services/api/api-client';
import { UserEntitlementDefinitionItemDto } from 'src/app/services/api/models/user-center-mod/user-entitlement-definition-item-dto.model';
import { BaseMatModules, CommonModules } from 'src/app/share/shared-modules';

interface UserEntitlementAddDialogData {
  userId: string;
  definitions: UserEntitlementDefinitionItemDto[];
}

@Component({
  selector: 'app-user-entitlement-add',
  imports: [...CommonModules, ...BaseMatModules, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  templateUrl: './add.html',
})
export class UserEntitlementAddComponent {
  private readonly api = inject(ApiClient);
  private readonly dialogRef = inject(MatDialogRef<UserEntitlementAddComponent>);
  readonly data = inject<UserEntitlementAddDialogData>(MAT_DIALOG_DATA);
  private readonly formBuilder = inject(FormBuilder);
  readonly form = this.formBuilder.nonNullable.group({
    entitlementDefinitionId: ['', Validators.required],
    valueLimit: [0, [Validators.required, Validators.min(0)]],
    expirationDate: [''],
    startDate: [new Date().toISOString().slice(0, 16), Validators.required],
  });

  save(): void {
    if (this.form.invalid) return;

    const value = this.form.getRawValue();
    this.api.userEntitlements.create(this.data.userId, {
      entitlementDefinitionId: value.entitlementDefinitionId,
      valueLimit: value.valueLimit,
      expirationDate: value.expirationDate ? new Date(value.expirationDate) : null,
      startDate: new Date(value.startDate),
    }).subscribe(() => this.dialogRef.close(true));
  }
}
