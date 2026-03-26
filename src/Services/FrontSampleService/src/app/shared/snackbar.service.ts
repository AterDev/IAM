import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { I18N_KEYS } from './i18n-keys';

@Injectable({ providedIn: 'root' })
export class SnackbarService {
  private readonly snack = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);
  private readonly i18n = I18N_KEYS;

  showSuccess(message: string) {
    this.snack.open(message, this.translate.instant(this.i18n.common.ok), { duration: 3000, panelClass: ['snackbar-success'] });
  }

  showError(message: string) {
    this.snack.open(message, this.translate.instant(this.i18n.common.close), { duration: 5000, panelClass: ['snackbar-error'] });
  }
}
