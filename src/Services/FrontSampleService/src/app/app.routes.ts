import { Routes } from '@angular/router';
import { AutoLoginPartialRoutesGuard } from 'angular-auth-oidc-client';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/home',
    pathMatch: 'full',
  },
  {
    path: 'home',
    loadComponent: () => import('./home/home.component').then((m) => m.HomeComponent),
  },
  {
    path: 'auth/callback',
    loadComponent: () => import('./callback/callback.component').then((m) => m.CallbackComponent),
  },
  {
    path: 'protected',
    loadComponent: () => import('./protected/protected.component').then((m) => m.ProtectedComponent),
    canActivate: [AutoLoginPartialRoutesGuard],
  },
  {
    path: 'authorizations',
    loadComponent: () => import('./authorizations/authorizations.component').then((m) => m.AuthorizationsComponent),
    canActivate: [AutoLoginPartialRoutesGuard],
  },
  {
    path: 'unauthorized',
    loadComponent: () => import('./unauthorized/unauthorized.component').then((m) => m.UnauthorizedComponent),
  },
  {
    path: '**',
    redirectTo: '/home',
  },
];
