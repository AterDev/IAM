import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink],
  template: `
    <div class="container">
      <header>
        <h1>🔐 IAM示例 - Angular应用</h1>
        <nav>
          <a routerLink="/home" routerLinkActive="active">首页</a>
          <a routerLink="/protected" routerLinkActive="active">受保护页面</a>
          @if (isAuthenticated) {
            <button (click)="logout()" class="btn-logout">登出</button>
            <span class="user-info">
              👤 {{ userData?.name || userData?.email || userData?.preferred_username || '用户' }}
            </span>
          } @else {
            <button (click)="login()" class="btn-login">登录</button>
          }
        </nav>
      </header>
      <main>
        <router-outlet></router-outlet>
      </main>
      <footer>
        <p>示例应用演示如何使用Angular对接IAM授权系统</p>
        <p class="tech-stack">
          <span>Angular 19</span>
          <span>•</span>
          <span>OpenID Connect</span>
          <span>•</span>
          <span>OAuth 2.0</span>
        </p>
      </footer>
    </div>
  `,
  styles: [`
    .container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 20px;
      min-height: 100vh;
      display: flex;
      flex-direction: column;
    }

    header {
      background: linear-gradient(135deg, #1976d2 0%, #1565c0 100%);
      color: white;
      padding: 20px;
      border-radius: 8px;
      margin-bottom: 20px;
      box-shadow: 0 4px 6px rgba(0,0,0,0.1);
    }

    h1 {
      margin: 0 0 15px 0;
      font-size: 24px;
    }

    nav {
      display: flex;
      gap: 15px;
      align-items: center;
      flex-wrap: wrap;
    }

    nav a {
      color: white;
      text-decoration: none;
      padding: 8px 16px;
      border-radius: 4px;
      transition: all 0.3s;
      font-weight: 500;
    }

    nav a:hover {
      background: rgba(255, 255, 255, 0.1);
    }

    nav a.active {
      background: rgba(255, 255, 255, 0.2);
      font-weight: bold;
    }

    button {
      border: none;
      padding: 8px 16px;
      border-radius: 4px;
      cursor: pointer;
      font-weight: bold;
      transition: all 0.3s;
      font-size: 14px;
    }

    .btn-login {
      background: white;
      color: #1976d2;
    }

    .btn-login:hover {
      background: #f0f0f0;
      transform: translateY(-2px);
      box-shadow: 0 4px 8px rgba(0,0,0,0.2);
    }

    .btn-logout {
      background: #f44336;
      color: white;
    }

    .btn-logout:hover {
      background: #d32f2f;
      transform: translateY(-2px);
      box-shadow: 0 4px 8px rgba(0,0,0,0.2);
    }

    .user-info {
      margin-left: auto;
      font-weight: bold;
      padding: 8px 12px;
      background: rgba(255, 255, 255, 0.2);
      border-radius: 4px;
    }

    main {
      background: white;
      padding: 30px;
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      min-height: 400px;
      flex: 1;
    }

    footer {
      text-align: center;
      margin-top: 20px;
      color: #666;
      padding: 20px;
    }

    footer p {
      margin: 5px 0;
    }

    .tech-stack {
      font-size: 14px;
      color: #999;
    }

    .tech-stack span {
      margin: 0 5px;
    }
  `]
})
export class AppComponent implements OnInit {
  private oidcSecurityService = inject(OidcSecurityService);
  
  isAuthenticated = false;
  userData: any;

  ngOnInit() {
    this.oidcSecurityService.checkAuth().subscribe(({ isAuthenticated, userData }) => {
      this.isAuthenticated = isAuthenticated;
      this.userData = userData;
      
      if (isAuthenticated) {
        console.log('用户已认证:', userData);
      }
    });
  }

  login() {
    this.oidcSecurityService.authorize();
  }

  logout() {
    this.oidcSecurityService.logoff().subscribe((result) => {
      console.log('登出成功');
    });
  }
}
