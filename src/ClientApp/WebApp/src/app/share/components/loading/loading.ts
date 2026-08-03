import { Component, input } from '@angular/core';
import { CommonModules } from 'src/app/share/shared-modules';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslateModule } from '@ngx-translate/core';

import { I18N_KEYS } from 'src/app/share/i18n-keys';
@Component({
  selector: 'app-loading',
  imports: [
    ...CommonModules,
    MatProgressSpinnerModule,
    TranslateModule
  ],
  template: `
    <div class="app-loading" role="status" aria-live="polite">
      <mat-spinner [diameter]="diameter()"></mat-spinner>
      <p class="loading-text">{{ i18n.common.loading | translate }}</p>
    </div>
  `,
  styles: [
    `
    .app-loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 12px;
      padding: 24px;
      color: var(--mat-sys-on-surface);
    }
    .loading-text {
      margin: 0;
      font-size: 14px;
      opacity: .8;
    }
    `
  ]
})
export class AppLoadingComponent {
  readonly i18n = I18N_KEYS;
  diameter = input<number>(40);
}
