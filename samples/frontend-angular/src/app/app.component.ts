import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatChipsModule } from '@angular/material/chips';
import { map } from 'rxjs/operators';
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
    MatChipsModule
  ],
  templateUrl: './app.component.html'
})
export class AppComponent implements OnInit {
  private oidcSecurityService = inject(OidcSecurityService);
  
  isAuthenticated = false;
  userData: any;
  darkMode = true;
  sidenavOpened = true;
  isInitialized = false;

  async ngOnInit() {
    // 等待库完全初始化
    const result = await firstValueFrom(this.oidcSecurityService.checkAuth());
    this.isInitialized = true;
    // 监听认证状态变化
    this.oidcSecurityService.isAuthenticated$
      .pipe(
        map((result) => result.isAuthenticated)
      )
      .subscribe((isAuthenticated) => {
        this.isAuthenticated = isAuthenticated;
      });

    this.oidcSecurityService.userData$.subscribe((userData) => {
      this.userData = userData;
      if (userData) {
        console.log('用户已认证:', userData);
      }
    });
  }

  login() {
    if (!this.isInitialized) {
      console.warn('OIDC库还未初始化，请稍候...');
      return;
    }
    this.oidcSecurityService.authorize();
  }

  logout() {
    this.oidcSecurityService.logoff().subscribe(() => {
      console.log('登出成功');
    });
  }

  toggleTheme() {
    this.darkMode = !this.darkMode;
    const body = document.body;
    if (this.darkMode) {
      body.classList.remove('light-theme');
      body.classList.add('dark-theme');
    } else {
      body.classList.remove('dark-theme');
      body.classList.add('light-theme');
    }
  }
}
