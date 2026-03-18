import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { TranslateModule } from '@ngx-translate/core';
import { BaseMatModules, CommonModules } from 'src/app/share/shared-modules';
import { I18N_KEYS } from 'src/app/share/i18n-keys';

@Component({
  selector: 'app-developer-portal-clients',
  imports: [
    ...BaseMatModules,
    ...CommonModules,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatListModule,
    TranslateModule,
  ],
  templateUrl: './clients.html',
  styleUrl: './clients.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeveloperPortalClientsComponent {
  readonly i18n = I18N_KEYS;

  readonly supportedItems: string[] = [
    'developerPortal.clientsSupported.listAndView',
    'developerPortal.clientsSupported.editConfiguration',
    'developerPortal.clientsSupported.rotateSecrets',
    'developerPortal.clientsSupported.reviewAuthorizations',
  ];

  readonly currentFlow: string[] = [
    'developerPortal.currentFlow.createOrOpen',
    'developerPortal.currentFlow.configure',
    'developerPortal.currentFlow.rotate',
  ];

  readonly boundaries: string[] = [
    'developerPortal.boundaries.noDedicatedRegistration',
    'developerPortal.boundaries.noCollaborators',
    'developerPortal.boundaries.noAnalytics',
  ];
}