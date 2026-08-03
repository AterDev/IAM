import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ApiClient } from 'src/app/services/api/api-client';
import { UserEntitlementDefinitionItemDto } from 'src/app/services/api/models/user-center-mod/user-entitlement-definition-item-dto.model';
import { I18N_KEYS } from 'src/app/share/i18n-keys';
import { BaseMatModules, CommonModules } from 'src/app/share/shared-modules';

interface UserEntitlementAddDialogData {
  userId: string;
  assignedDefinitionIds: string[];
}

@Component({
  selector: 'app-user-entitlement-add',
  imports: [...CommonModules, ...BaseMatModules, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  templateUrl: './add.html',
})
export class UserEntitlementAddComponent implements OnInit {
  readonly i18n = I18N_KEYS;
  private readonly api = inject(ApiClient);
  private readonly dialogRef = inject(MatDialogRef<UserEntitlementAddComponent>);
  readonly data = inject<UserEntitlementAddDialogData>(MAT_DIALOG_DATA);
  readonly definitions = signal<UserEntitlementDefinitionItemDto[]>([]);
  readonly loading = signal(true);
  private readonly formBuilder = inject(FormBuilder);
  readonly form = this.formBuilder.nonNullable.group({
    entitlementDefinitionId: ['', Validators.required],
    valueLimit: [0, [Validators.required, Validators.min(0)]],
    expirationDate: [''],
    startDate: [new Date().toISOString().slice(0, 16), Validators.required],
  });

  ngOnInit(): void {
    this.api.userEntitlementDefinitions.getPage(null, 1, 100, null).subscribe({
      next: page => {
        const assigned = new Set(this.data.assignedDefinitionIds);
        this.definitions.set(page.data.filter(item => !assigned.has(item.id)));
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

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
