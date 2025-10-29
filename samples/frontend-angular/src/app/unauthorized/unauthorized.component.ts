import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="unauthorized">
      <h2>Unauthorized</h2>
      <p>You do not have permission to access this resource.</p>
      <p>Please log in with appropriate credentials.</p>
      <a routerLink="/home" class="btn">Go to Home</a>
    </div>
  `,
  styles: [`
    .unauthorized {
      padding: 20px;
      text-align: center;
    }

    h2 {
      color: #f44336;
      margin-bottom: 20px;
    }

    p {
      margin: 10px 0;
      color: #666;
    }

    .btn {
      display: inline-block;
      margin-top: 20px;
      padding: 12px 24px;
      background: #1976d2;
      color: white;
      text-decoration: none;
      border-radius: 4px;
    }

    .btn:hover {
      background: #1565c0;
    }
  `]
})
export class UnauthorizedComponent {}
