import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { TranslateModule } from '@ngx-translate/core';
import { BaseMatModules, CommonModules } from 'src/app/share/shared-modules';
import { I18N_KEYS } from 'src/app/share/i18n-keys';

type TranslationItem = {
  title: string;
  description: string;
};

@Component({
  selector: 'app-developer-portal-overview',
  imports: [
    ...BaseMatModules,
    ...CommonModules,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDividerModule,
    MatIconModule,
    MatListModule,
    TranslateModule,
  ],
  templateUrl: './overview.html',
  styleUrl: './overview.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeveloperPortalOverviewComponent {
  readonly i18n = I18N_KEYS;

  readonly currentCapabilities: TranslationItem[] = [
    {
      title: 'developerPortal.currentCapabilities.manageApplicationsTitle',
      description: 'developerPortal.currentCapabilities.manageApplicationsDescription',
    },
    {
      title: 'developerPortal.currentCapabilities.configureUrisTitle',
      description: 'developerPortal.currentCapabilities.configureUrisDescription',
    },
    {
      title: 'developerPortal.currentCapabilities.rotateSecretTitle',
      description: 'developerPortal.currentCapabilities.rotateSecretDescription',
    },
  ];

  readonly upcomingCapabilities: string[] = [
    'developerPortal.upcoming.dynamicRegistration',
    'developerPortal.upcoming.approvalWorkflow',
    'developerPortal.upcoming.collaboration',
    'developerPortal.upcoming.usageInsights',
  ];
}