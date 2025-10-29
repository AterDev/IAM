import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { OidcSecurityService } from 'angular-auth-oidc-client';

@Component({
  selector: 'app-protected',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="protected">
      <h2>Protected Resource</h2>
      
      <div class="user-info">
        <h3>User Information</h3>
        @if (userData) {
          <dl>
            <dt>Name:</dt>
            <dd>{{ userData.name || 'N/A' }}</dd>
            <dt>Email:</dt>
            <dd>{{ userData.email || 'N/A' }}</dd>
            <dt>Subject:</dt>
            <dd>{{ userData.sub || 'N/A' }}</dd>
          </dl>
        }
      </div>

      <div class="api-test">
        <h3>API Test</h3>
        <p>Test calling protected API endpoint with access token:</p>
        <button (click)="callApi()" [disabled]="loading">
          {{ loading ? 'Loading...' : 'Call Protected API' }}
        </button>

        @if (apiResponse) {
          <div class="response success">
            <h4>API Response:</h4>
            <pre>{{ apiResponse | json }}</pre>
          </div>
        }

        @if (error) {
          <div class="response error">
            <h4>Error:</h4>
            <p>{{ error }}</p>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .protected {
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

    .user-info {
      background: #e8f5e9;
      padding: 20px;
      border-radius: 8px;
      margin-bottom: 20px;
    }

    dl {
      display: grid;
      grid-template-columns: 120px 1fr;
      gap: 10px;
    }

    dt {
      font-weight: bold;
    }

    dd {
      margin: 0;
    }

    .api-test {
      margin-top: 30px;
    }

    button {
      background: #1976d2;
      color: white;
      border: none;
      padding: 12px 24px;
      border-radius: 4px;
      cursor: pointer;
      font-size: 16px;
      margin: 10px 0;
    }

    button:hover:not(:disabled) {
      background: #1565c0;
    }

    button:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .response {
      margin-top: 20px;
      padding: 15px;
      border-radius: 8px;
    }

    .response.success {
      background: #e8f5e9;
      border: 1px solid #4caf50;
    }

    .response.error {
      background: #ffebee;
      border: 1px solid #f44336;
    }

    pre {
      overflow-x: auto;
      white-space: pre-wrap;
      word-wrap: break-word;
    }
  `]
})
export class ProtectedComponent implements OnInit {
  private oidcSecurityService = inject(OidcSecurityService);
  private http = inject(HttpClient);

  userData: any;
  apiResponse: any;
  error: string = '';
  loading = false;

  ngOnInit() {
    this.oidcSecurityService.userData$.subscribe(userData => {
      this.userData = userData;
    });
  }

  callApi() {
    this.loading = true;
    this.error = '';
    this.apiResponse = null;

    // Call the protected API endpoint
    this.http.get('https://localhost:5001/api/weatherforecast')
      .subscribe({
        next: (response) => {
          this.apiResponse = response;
          this.loading = false;
        },
        error: (err) => {
          this.error = err.error?.message || err.message || 'Failed to call API';
          this.loading = false;
        }
      });
  }
}
