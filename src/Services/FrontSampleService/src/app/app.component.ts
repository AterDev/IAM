import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { firstValueFrom } from 'rxjs';
import { map } from 'rxjs/operators';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { SamplePermissionsService } from './shared/sample-permissions.service';

interface NavigationItem {
  label: string;
  icon: string;
  path: string;
  requiresAuth?: boolean;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatSidenavModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatDividerModule,
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent implements OnInit {
  private readonly oidcSecurityService = inject(OidcSecurityService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly permissionsService = inject(SamplePermissionsService);

  isAuthenticated = false;
  userData: Record<string, unknown> | null = null;
  sidenavOpened = true;
  isInitialized = false;
  displayName = '访客';

  readonly navigationItems: NavigationItem[] = [
    { label: '首页', icon: 'home', path: '/home' },
    { label: '受保护页面', icon: 'shield', path: '/protected', requiresAuth: true },
    { label: '我的授权', icon: 'fact_check', path: '/authorizations', requiresAuth: true },
  ];

  readonly permissionEntries = this.permissionsService.permissions;
  readonly permissionsLoading = this.permissionsService.loading;

  async ngOnInit() {
    if (!this.isAuthCallbackRoute()) {
      try {
        await firstValueFrom(this.oidcSecurityService.checkAuth());
      } catch (error) {
        console.error('OIDC 初始化失败', error);
      } finally {
        this.isInitialized = true;
      }
    } else {
      this.isInitialized = true;
    }

    this.oidcSecurityService.isAuthenticated$
      .pipe(
        map((result: { isAuthenticated: boolean }) => result.isAuthenticated),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((isAuthenticated: boolean) => {
        queueMicrotask(() => {
          this.isAuthenticated = isAuthenticated;

          if (isAuthenticated) {
            this.permissionsService.loadForCurrentUser();
            return;
          }

          this.permissionsService.reset();
        });
      });

    this.oidcSecurityService.userData$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((userData: { userData?: Record<string, unknown> } | Record<string, unknown> | null) => {
        const normalizedUserData = (
          userData && typeof userData === 'object' && 'userData' in userData
            ? userData.userData
            : userData
        ) as Record<string, unknown> | null | undefined;

        queueMicrotask(() => {
          this.userData = normalizedUserData ?? null;
          this.displayName = this.pickDisplayName(this.userData) ?? '用户';
        });
      });
  }

  login() {
    if (!this.isInitialized) {
      return;
    }

    this.oidcSecurityService.authorize();
  }

  logout() {
    this.oidcSecurityService.logoff().subscribe();
  }

  private isAuthCallbackRoute(): boolean {
    return this.router.url.startsWith('/auth/callback')
      || window.location.pathname.startsWith('/auth/callback');
  }

  private pickDisplayName(userData: Record<string, unknown> | null): string | null {
    if (!userData) {
      return null;
    }

    const candidates = [
      userData['name'],
      userData['preferred_username'],
      userData['email'],
      userData['sub'],
    ];

    for (const candidate of candidates) {
      if (typeof candidate === 'string' && candidate.trim().length > 0) {
        return candidate;
      }
    }

    return null;
  }
}
