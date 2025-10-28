import { inject, Injectable } from '@angular/core';
import { AuditTrailService } from './services/audit-trail.service';
import { ClientsService } from './services/clients.service';
import { CommonService } from './services/common.service';
import { CommonSettingsService } from './services/common-settings.service';
import { ExternalAuthService } from './services/external-auth.service';
import { OAuthService } from './services/oauth.service';
import { OrganizationsService } from './services/organizations.service';
import { ResourcesService } from './services/resources.service';
import { RolesService } from './services/roles.service';
import { ScopesService } from './services/scopes.service';
import { SecurityService } from './services/security.service';
import { UsersService } from './services/users.service';
import { SecurityService } from './services/security.service';
@Injectable({
  providedIn: 'root'
})
export class ApiClient {
  public auditTrail = inject(AuditTrailService);
  public clients = inject(ClientsService);
  public common = inject(CommonService);
  public commonSettings = inject(CommonSettingsService);
  public externalAuth = inject(ExternalAuthService);
  public oAuth = inject(OAuthService);
  public organizations = inject(OrganizationsService);
  public resources = inject(ResourcesService);
  public roles = inject(RolesService);
  public scopes = inject(ScopesService);
  public security = inject(SecurityService);
  public users = inject(UsersService);
  public security = inject(SecurityService);
}
