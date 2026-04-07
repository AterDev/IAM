import { Routes } from '@angular/router';
import { Login } from './pages/login/login';
import { LayoutComponent } from './layout/layout';
import { Notfound } from './pages/notfound/notfound';
import { AuthGuard } from './share/auth.guard';

export const routes: Routes = [
  { path: 'login', component: Login },
  {
    path: 'auth/callback',
    loadComponent: () => import('./pages/auth-callback/callback').then(m => m.AuthCallbackComponent)
  },
  {
    path: 'external-auth/callback',
    loadComponent: () => import('./pages/external-auth-callback/external-auth-callback').then(m => m.ExternalAuthCallbackComponent)
  },
  { 
    path: 'register', 
    loadComponent: () => import('./pages/register/register').then(m => m.Register)
  },
  { 
    path: 'forgot-password', 
    loadComponent: () => import('./pages/forgot-password/forgot-password').then(m => m.ForgotPassword)
  },
  { 
    path: 'device-code', 
    loadComponent: () => import('./pages/device-code/device-code').then(m => m.DeviceCode)
  },
  { 
    path: 'authorize', 
    loadComponent: () => import('./pages/authorize/authorize').then(m => m.Authorize)
  },
  {
    path: '',
    component: LayoutComponent,
    canActivate: [AuthGuard],
    canActivateChild: [AuthGuard],
    children: [
      {
        path: '',
        redirectTo: 'user/list',
        pathMatch: 'full'
      },
      {
        path: 'user/list',
        loadComponent: () => import('./pages/user/list/list').then(m => m.UserListComponent)
      },
      {
        path: 'user/detail/:id',
        loadComponent: () => import('./pages/user/detail/detail').then(m => m.UserDetailComponent)
      },
      {
        path: 'organization/list',
        loadComponent: () => import('./pages/organization/list/list').then(m => m.OrganizationListComponent)
      },
      {
        path: 'role/list',
        loadComponent: () => import('./pages/role/list/list').then(m => m.RoleListComponent)
      },
      {
        path: 'role/permissions/:id',
        loadComponent: () => import('./pages/role/permissions/permissions').then(m => m.RolePermissionsComponent)
      },
      {
        path: 'role/detail/:id',
        loadComponent: () => import('./pages/role/detail/detail').then(m => m.RoleDetailComponent)
      },
      {
        path: 'client/list',
        loadComponent: () => import('./pages/client/list/list').then(m => m.ClientListComponent)
      },
      {
        path: 'client/detail/:id',
        loadComponent: () => import('./pages/client/detail/detail').then(m => m.ClientDetailComponent)
      },
      {
        path: 'permission/list',
        loadComponent: () => import('./pages/permission/list/list').then(m => m.PermissionListComponent)
      },
      {
        path: 'scope/list',
        loadComponent: () => import('./pages/scope/list/list').then(m => m.ScopeListComponent)
      },
      {
        path: 'scope/detail/:id',
        loadComponent: () => import('./pages/scope/detail/detail').then(m => m.ScopeDetailComponent)
      },
      {
        path: 'resource/list',
        loadComponent: () => import('./pages/resource/list/list').then(m => m.ResourceListComponent)
      },
      {
        path: 'resource/detail/:id',
        loadComponent: () => import('./pages/resource/detail/detail').then(m => m.ResourceDetailComponent)
      },
      {
        path: 'security/session-list',
        loadComponent: () => import('./pages/security/session-list/list').then(m => m.SessionListComponent)
      },
      {
        path: 'security/session-list/:id',
        loadComponent: () => import('./pages/security/session-list/list').then(m => m.SessionListComponent)
      },
      {
        path: 'security/audit-log-list',
        loadComponent: () => import('./pages/security/audit-log-list/list').then(m => m.AuditLogListComponent),
        data: { auditTab: 'logs' }
      },
      {
        path: 'security/password-grant-audit',
        loadComponent: () => import('./pages/security/password-grant-audit/password-grant-audit').then(m => m.PasswordGrantAuditComponent)
      },
      {
        path: 'security/change-password',
        loadComponent: () => import('./pages/security/change-password/change-password').then(m => m.ChangePasswordComponent)
      },
      {
        path: 'security/mfa',
        loadComponent: () => import('./pages/security/mfa/mfa').then(m => m.MfaSettingsComponent)
      }
    ],
  },
  
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: '**', component: Notfound },
];
