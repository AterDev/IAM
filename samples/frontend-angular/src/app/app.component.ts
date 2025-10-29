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
        <h1>IAM Sample Angular Application</h1>
        <nav>
          <a routerLink="/home">Home</a>
          <a routerLink="/protected">Protected</a>
          @if (isAuthenticated) {
            <button (click)="logout()">Logout</button>
            <span class="user-info">{{ userData?.name || userData?.email }}</span>
          } @else {
            <button (click)="login()">Login</button>
          }
        </nav>
      </header>
      <main>
        <router-outlet></router-outlet>
      </main>
      <footer>
        <p>Sample application demonstrating IAM integration with Angular</p>
      </footer>
    </div>
  `,
  styles: [`
    .container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 20px;
    }

    header {
      background: #1976d2;
      color: white;
      padding: 20px;
      border-radius: 8px;
      margin-bottom: 20px;
    }

    h1 {
      margin: 0 0 15px 0;
    }

    nav {
      display: flex;
      gap: 15px;
      align-items: center;
    }

    nav a {
      color: white;
      text-decoration: none;
      padding: 8px 16px;
      border-radius: 4px;
      transition: background 0.3s;
    }

    nav a:hover {
      background: rgba(255, 255, 255, 0.1);
    }

    button {
      background: white;
      color: #1976d2;
      border: none;
      padding: 8px 16px;
      border-radius: 4px;
      cursor: pointer;
      font-weight: bold;
    }

    button:hover {
      background: #f0f0f0;
    }

    .user-info {
      margin-left: auto;
      font-weight: bold;
    }

    main {
      background: white;
      padding: 30px;
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      min-height: 400px;
    }

    footer {
      text-align: center;
      margin-top: 20px;
      color: #666;
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
    });
  }

  login() {
    this.oidcSecurityService.authorize();
  }

  logout() {
    this.oidcSecurityService.logoff().subscribe();
  }
}
