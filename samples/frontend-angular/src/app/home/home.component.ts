import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OidcSecurityService } from 'angular-auth-oidc-client';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="home">
      <h2>Welcome to IAM Sample Application</h2>
      
      @if (isAuthenticated$ | async) {
        <div class="welcome-message">
          <p>You are logged in!</p>
          <p>You can now access protected resources.</p>
        </div>
      } @else {
        <div class="info">
          <p>This is a sample application demonstrating integration with IAM using Angular and OIDC.</p>
          <p>Click "Login" in the navigation to authenticate.</p>
        </div>
      }

      <div class="features">
        <h3>Features</h3>
        <ul>
          <li>OAuth 2.0 / OpenID Connect authentication</li>
          <li>Automatic token management</li>
          <li>Protected routes with authentication guard</li>
          <li>HTTP interceptor for automatic token injection</li>
          <li>Silent token renewal</li>
        </ul>
      </div>

      <div class="getting-started">
        <h3>Getting Started</h3>
        <ol>
          <li>Ensure IAM server is running at <code>https://localhost:7001</code></li>
          <li>Register a client in IAM with client ID: <code>sample-angular-client</code></li>
          <li>Configure redirect URIs: <code>http://localhost:4200</code></li>
          <li>Add allowed scopes: <code>openid profile email sample-api</code></li>
          <li>Click "Login" to start the authentication flow</li>
        </ol>
      </div>
    </div>
  `,
  styles: [`
    .home {
      padding: 20px;
    }

    h2 {
      color: #1976d2;
      margin-bottom: 20px;
    }

    h3 {
      color: #333;
      margin-top: 30px;
      margin-bottom: 15px;
    }

    .welcome-message {
      background: #e8f5e9;
      padding: 20px;
      border-radius: 8px;
      margin-bottom: 20px;
    }

    .info {
      background: #e3f2fd;
      padding: 20px;
      border-radius: 8px;
      margin-bottom: 20px;
    }

    .features ul, .getting-started ol {
      line-height: 1.8;
    }

    code {
      background: #f5f5f5;
      padding: 2px 6px;
      border-radius: 3px;
      font-family: 'Courier New', monospace;
    }
  `]
})
export class HomeComponent {
  private oidcSecurityService = inject(OidcSecurityService);
  isAuthenticated$ = this.oidcSecurityService.isAuthenticated$;
}
