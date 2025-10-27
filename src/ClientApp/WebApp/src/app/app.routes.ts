import { Routes } from '@angular/router';
import { Login } from './pages/login/login';
import { LayoutComponent } from './layout/layout';
import { Notfound } from './pages/notfound/notfound';
import { AuthGuard } from './share/auth.guard';

export const routes: Routes = [
  { path: 'login', component: Login },
  {
    path: '',
    component: LayoutComponent,
    canActivate: [AuthGuard],
    canActivateChild: [AuthGuard],
    children: [
      {
        path: 'system-role',
        data: { breadcrumb: 'systemRole.title' },
        children: [
          { path: '', redirectTo: '/system-role/index', pathMatch: 'full' },
          { 
            path: 'index', 
            loadComponent: () => import('./pages/system-role/index/index').then(m => m.Index),
            data: { breadcrumb: 'systemRole.list' }
          },
        ]
      },
      {
        path: 'system-user',
        data: { breadcrumb: 'systemUser.title' },
        children: [
          { path: '', redirectTo: '/system-user/index', pathMatch: 'full' },
          { 
            path: 'index', 
            loadComponent: () => import('./pages/system-user/index/index').then(m => m.Index),
            data: { breadcrumb: 'systemUser.list' }
          },
        ]
      },
      {
        path: 'system-logs',
        data: { breadcrumb: 'systemLogs.title' },
        children: [
          { path: '', redirectTo: '/system-logs/index', pathMatch: 'full' },
          { 
            path: 'index', 
            loadComponent: () => import('./pages/system-logs/index/index').then(m => m.Index),
            data: { breadcrumb: 'systemLogs.list' }
          },
        ]
      },
    ],
  },
  
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: '**', component: Notfound },
];
