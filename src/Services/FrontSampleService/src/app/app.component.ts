import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { map } from 'rxjs/operators';
import { RouterLink, RouterOutlet } from '@angular/router';
import { firstValueFrom } from 'rxjs';

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

  isAuthenticated = false;
  userData: Record<string, unknown> | null = null;
  sidenavOpened = true;
  isInitialized = false;

  async ngOnInit() {
    try {
      await firstValueFrom(this.oidcSecurityService.checkAuth());
      this.isInitialized = true;

      this.oidcSecurityService.isAuthenticated$
        .pipe(map((result: { isAuthenticated: boolean }) => result.isAuthenticated))
        .subscribe((isAuthenticated: boolean) => {
          this.isAuthenticated = isAuthenticated;
        });

      this.oidcSecurityService.userData$.subscribe((userData: { userData?: Record<string, unknown> } | Record<string, unknown> | null) => {
        const normalizedUserData = (
          userData && typeof userData === 'object' && 'userData' in userData
            ? userData.userData
            : userData
        ) as Record<string, unknown> | null | undefined;

        this.userData = normalizedUserData ?? null;
      });
    } catch (error) {
      console.error('OIDC 初始化失败', error);
      this.isInitialized = true;
    }
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
}
