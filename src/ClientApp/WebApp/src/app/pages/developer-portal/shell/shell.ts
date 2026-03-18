import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import { BaseMatModules, CommonModules } from 'src/app/share/shared-modules';
import { I18N_KEYS } from 'src/app/share/i18n-keys';

@Component({
  selector: 'app-developer-portal-shell',
  imports: [
    ...BaseMatModules,
    ...CommonModules,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    TranslateModule,
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeveloperPortalShellComponent {
  readonly i18n = I18N_KEYS;
}