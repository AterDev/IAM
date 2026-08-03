import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { ApiClient } from 'src/app/services/api/api-client';
import { UserEntitlementType } from 'src/app/services/api/models/entity/user-entitlement-type.model';
import { I18N_KEYS } from 'src/app/share/i18n-keys';
import { BaseMatModules, CommonModules } from 'src/app/share/shared-modules';

@Component({
  selector: 'app-entitlement-definition-add',
  imports: [
    ...CommonModules,
    ...BaseMatModules,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './add.html',
})
export class EntitlementDefinitionAddComponent {
  readonly i18n = I18N_KEYS;
  private readonly api = inject(ApiClient);
  private readonly dialog = inject(MatDialogRef<EntitlementDefinitionAddComponent>);
  private readonly snack = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);
  private readonly formBuilder = inject(FormBuilder);

  readonly type = UserEntitlementType;
  readonly form = this.formBuilder.nonNullable.group({
    displayName: ['', Validators.required],
    description: [''],
    entitlementCode: ['', Validators.required],
    entitlementType: [UserEntitlementType.ProxyUsage],
    unit: ['', Validators.required],
  });

  save(): void {
    if (this.form.invalid) return;

    this.api.userEntitlementDefinitions.create(this.form.getRawValue()).subscribe({
      next: () => this.dialog.close(true),
      error: () => this.snack.open(
        this.translate.instant(this.i18n.entitlement.saveFailed),
        this.translate.instant(this.i18n.common.close),
        { duration: 3000 },
      ),
    });
  }
}
