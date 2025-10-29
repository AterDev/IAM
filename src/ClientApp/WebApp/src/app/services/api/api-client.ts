import { inject, Injectable } from '@angular/core';
import { AdminAuthService } from './services/admin-auth.service';
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
@Injectable({
  providedIn: 'root'
})
export class ApiClient {
  /** Admin authentication controller for management portal login */
  public adminAuth = inject(AdminAuthService);
  /** Audit trail controller */
  public auditTrail = inject(AuditTrailService);
  /** OAuth/OIDC client management controller */
  public clients = inject(ClientsService);
  /** CommonController */
  public common = inject(CommonService);
  /** Common settings controller */
  public commonSettings = inject(CommonSettingsService);
  /** ExternalAuth */
  public externalAuth = inject(ExternalAuthService);
  /** OAuth 2.0 / OpenID Connect endpoint controller */
  public oAuth = inject(OAuthService);
  /** Organization management controller */
  public organizations = inject(OrganizationsService);
  /** API resource management controller */
  public resources = inject(ResourcesService);
  /** Role management controller */
  public roles = inject(RolesService);
  /** API scope management controller */
  public scopes = inject(ScopesService);
  /** Security controller for session and audit log management */
  public security = inject(SecurityService);
  /** User management controller */
  public users = inject(UsersService);
}
