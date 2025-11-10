import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="unauthorized">
      <div class="icon">🔒</div>
      <h2>未授权</h2>
      <p>您没有权限访问此资源。</p>
      <p>请使用适当的凭据登录。</p>
      <a routerLink="/home" class="btn">返回首页</a>
    </div>
  `,
  styles: [`
    .unauthorized {
      padding: 40px 20px;
      text-align: center;
      max-width: 500px;
      margin: 0 auto;
    }

    .icon {
      font-size: 64px;
      margin-bottom: 20px;
    }

    h2 {
      color: #f44336;
      margin-bottom: 20px;
      font-size: 28px;
    }

    p {
      margin: 10px 0;
      color: #666;
      font-size: 16px;
      line-height: 1.6;
    }

    .btn {
      display: inline-block;
      margin-top: 30px;
      padding: 12px 24px;
      background: #1976d2;
      color: white;
      text-decoration: none;
      border-radius: 4px;
      font-weight: 500;
      transition: all 0.3s;
    }

    .btn:hover {
      background: #1565c0;
      transform: translateY(-2px);
      box-shadow: 0 4px 8px rgba(0,0,0,0.2);
    }
  `]
})
export class UnauthorizedComponent {}
