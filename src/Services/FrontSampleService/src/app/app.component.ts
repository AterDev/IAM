import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { firstValueFrom } from 'rxjs';
import { map } from 'rxjs/operators';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    MatToolbarModule,
    MatSidenavModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatChipsModule,
  ],
  templateUrl: './app.component.html',
})
export class AppComponent implements OnInit {
  private readonly oidcSecurityService = inject(OidcSecurityService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);

  isAuthenticated = false;
  userData: Record<string, unknown> | null = null;
  sidenavOpened = true;
  isInitialized = false;

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
}
