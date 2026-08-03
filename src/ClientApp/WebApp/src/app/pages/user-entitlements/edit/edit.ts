import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { ApiClient } from 'src/app/services/api/api-client';
import { UserEntitlementDetailDto } from 'src/app/services/api/models/user-center-mod/user-entitlement-detail-dto.model';
import { I18N_KEYS } from 'src/app/share/i18n-keys';
import { BaseMatModules, CommonModules } from 'src/app/share/shared-modules';

@Component({
  selector: 'app-user-entitlement-edit',
  imports: [...CommonModules, ...BaseMatModules, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule],
  templateUrl: './edit.html',
})
export class UserEntitlementEditComponent {
  readonly i18n = I18N_KEYS;
  private readonly api = inject(ApiClient);
  private readonly dialogRef = inject(MatDialogRef<UserEntitlementEditComponent>);
  private readonly snackBar = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);
  private readonly formBuilder = inject(FormBuilder);
  readonly data = inject<UserEntitlementDetailDto>(MAT_DIALOG_DATA);
  readonly form = this.formBuilder.nonNullable.group({
    valueLimit: [this.data.valueLimit, [Validators.required, Validators.min(0)]],
    expirationDate: [this.toDateTimeLocal(this.data.expirationDate)],
    startDate: [this.toDateTimeLocal(this.data.startDate), Validators.required],
  });

  save(): void {
    if (this.form.invalid) return;

    const value = this.form.getRawValue();
    this.api.userEntitlements.update(this.data.id, {
      valueLimit: value.valueLimit,
      expirationDate: value.expirationDate ? new Date(value.expirationDate) : null,
      startDate: new Date(value.startDate),
    }).subscribe({
      next: () => this.dialogRef.close(true),
      error: () => this.snackBar.open(
        this.translate.instant(this.i18n.entitlement.saveFailed),
        this.translate.instant(this.i18n.common.close),
        { duration: 3000 },
      ),
    });
  }

  private toDateTimeLocal(value: Date | null | undefined): string {
    if (!value) return '';

    const date = new Date(value);
    const pad = (part: number) => part.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }
}
