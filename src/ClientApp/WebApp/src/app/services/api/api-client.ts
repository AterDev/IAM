import { inject, Injectable } from '@angular/core';
import { AccountService } from './services/account.service';
import { AdminAuthService } from './services/admin-auth.service';
import { AuditTrailService } from './services/audit-trail.service';
import { AuthorizationService } from './services/authorization.service';
import { ClientsService } from './services/clients.service';
import { DiscoveryService } from './services/discovery.service';
import { ExternalAuthService } from './services/external-auth.service';
import { OAuthService } from './services/oauth.service';
import { OAuthInteractionService } from './services/oauth-interaction.service';
import { OrganizationsService } from './services/organizations.service';
import { PermissionsService } from './services/permissions.service';
import { ResourcesService } from './services/resources.service';
import { RolesService } from './services/roles.service';
import { ScopesService } from './services/scopes.service';
import { SecurityService } from './services/security.service';
import { UsersService } from './services/users.service';
@Injectable({
  providedIn: 'root'
})
export class ApiClient {
  /** Self-service account endpoints for public authentication flows. */
  public account = inject(AccountService);
  /** Admin authentication controller for management portal login */
  public adminAuth = inject(AdminAuthService);
  /** Audit trail controller */
  public auditTrail = inject(AuditTrailService);
  /** Authorization management controller for users to view and manage their authorizations */
  public authorization = inject(AuthorizationService);
  /** OAuth/OIDC client management controller */
  public clients = inject(ClientsService);
  /** OpenID Connect Discovery endpoint controller */
  public discovery = inject(DiscoveryService);
  /** ExternalAuth */
  public externalAuth = inject(ExternalAuthService);
  /** OAuth 2.0 / OpenID Connect endpoint controller */
  public oAuth = inject(OAuthService);
  /** Interaction endpoints used by the SPA authorize and device-code pages. */
  public oAuthInteraction = inject(OAuthInteractionService);
  /** Organization management controller */
  public organizations = inject(OrganizationsService);
  /** Unified permission management controller. */
  public permissions = inject(PermissionsService);
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
