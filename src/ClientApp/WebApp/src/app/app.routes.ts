import { Routes } from '@angular/router';
import { Login } from './pages/login/login';
import { LayoutComponent } from './layout/layout';
import { Notfound } from './pages/notfound/notfound';
import { AuthGuard } from './share/auth.guard';

export const routes: Routes = [
  { path: 'login', component: Login },
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
        path: 'user',
        loadComponent: () => import('./pages/user/list/list').then(m => m.UserListComponent)
      },
      {
        path: 'user/:id',
        loadComponent: () => import('./pages/user/detail/detail').then(m => m.UserDetailComponent)
      },
      {
        path: 'organization',
        loadComponent: () => import('./pages/organization/list/list').then(m => m.OrganizationListComponent)
      },
      {
        path: 'role',
        loadComponent: () => import('./pages/role/list/list').then(m => m.RoleListComponent)
      },
      {
        path: 'role/:id',
        loadComponent: () => import('./pages/role/detail/detail').then(m => m.RoleDetailComponent)
      },
      {
        path: 'client',
        loadComponent: () => import('./pages/client/list/list').then(m => m.ClientListComponent)
      },
      {
        path: 'client/:id',
        loadComponent: () => import('./pages/client/detail/detail').then(m => m.ClientDetailComponent)
      },
      {
        path: 'scope',
        loadComponent: () => import('./pages/scope/list/list').then(m => m.ScopeListComponent)
      },
      {
        path: 'scope/:id',
        loadComponent: () => import('./pages/scope/detail/detail').then(m => m.ScopeDetailComponent)
      },
      {
        path: 'resource',
        loadComponent: () => import('./pages/resource/list/list').then(m => m.ResourceListComponent)
      },
      {
        path: 'resource/:id',
        loadComponent: () => import('./pages/resource/detail/detail').then(m => m.ResourceDetailComponent)
      },
      {
        path: 'security/sessions',
        loadComponent: () => import('./pages/security/session-list/list').then(m => m.SessionListComponent)
      },
      {
        path: 'security/audit-logs',
        loadComponent: () => import('./pages/security/audit-log-list/list').then(m => m.AuditLogListComponent)
      }
    ],
  },
  
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: '**', component: Notfound },
];
