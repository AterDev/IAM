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
        path: 'system-user',
        loadComponent: () => import('./pages/system-user/user-list').then(m => m.UserListComponent)
      },
      {
        path: 'system-user/:id',
        loadComponent: () => import('./pages/system-user/user-detail').then(m => m.UserDetailComponent)
      },
      {
        path: 'organization',
        loadComponent: () => import('./pages/organization/organization-list').then(m => m.OrganizationListComponent)
      },
      {
        path: 'system-role',
        loadComponent: () => import('./pages/system-role/role-list').then(m => m.RoleListComponent)
      },
      {
        path: 'system-role/:id',
        loadComponent: () => import('./pages/system-role/role-detail').then(m => m.RoleDetailComponent)
      },
      {
        path: 'scope',
        loadComponent: () => import('./pages/scope/scope-list').then(m => m.ScopeListComponent)
      },
      // TODO: Implement system-logs pages
    ],
  },
  
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: '**', component: Notfound },
];
