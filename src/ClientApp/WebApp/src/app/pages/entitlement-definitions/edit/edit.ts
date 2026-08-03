import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { ApiClient } from 'src/app/services/api/api-client';
import { UserEntitlementType } from 'src/app/services/api/models/entity/user-entitlement-type.model';
import { UserEntitlementDefinitionItemDto } from 'src/app/services/api/models/user-center-mod/user-entitlement-definition-item-dto.model';
import { I18N_KEYS } from 'src/app/share/i18n-keys';
import { BaseMatModules, CommonModules } from 'src/app/share/shared-modules';

@Component({
  selector: 'app-entitlement-definition-edit',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './edit.html',
})
export class EntitlementDefinitionEditComponent {
  readonly i18n = I18N_KEYS;
  private readonly api = inject(ApiClient);
  private readonly dialog = inject(MatDialogRef<EntitlementDefinitionEditComponent>);
  private readonly snack = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);
  private readonly formBuilder = inject(FormBuilder);

  readonly data = inject<UserEntitlementDefinitionItemDto>(MAT_DIALOG_DATA);
  readonly type = UserEntitlementType;
  readonly form = this.formBuilder.nonNullable.group({
    displayName: [this.data.displayName, Validators.required],
    description: [this.data.description ?? ''],
    entitlementCode: [this.data.entitlementCode, Validators.required],
    entitlementType: [this.data.entitlementType],
    unit: [this.data.unit, Validators.required],
  });

  save(): void {
    if (this.form.invalid) return;

    this.api.userEntitlementDefinitions.update(this.data.id, this.form.getRawValue()).subscribe({
      next: () => this.dialog.close(true),
      error: () => this.snack.open(
        this.translate.instant(this.i18n.entitlement.saveFailed),
        this.translate.instant(this.i18n.common.close),
        { duration: 3000 },
      ),
    });
  }
}
