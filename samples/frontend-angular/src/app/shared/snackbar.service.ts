import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({ providedIn: 'root' })
export class SnackbarService {
  private snack = inject(MatSnackBar);

  showSuccess(message: string) {
    this.snack.open(message, 'OK', { duration: 3000, panelClass: ['snackbar-success'] });
  }

  showError(message: string) {
    this.snack.open(message, '关闭', { duration: 5000, panelClass: ['snackbar-error'] });
  }
}
