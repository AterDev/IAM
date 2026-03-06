import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatListModule } from '@angular/material/list';
import { OidcSecurityService } from 'angular-auth-oidc-client';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatListModule, MatChipsModule],
  templateUrl: './home.component.html',
})
export class HomeComponent implements OnInit {
  private readonly oidcSecurityService = inject(OidcSecurityService);
  readonly isAuthenticated$ = this.oidcSecurityService.isAuthenticated$;

  ngOnInit(): void {
    this.oidcSecurityService.getAccessToken().subscribe((token: string) => {
      if (token) {
        console.debug('已获取访问令牌');
      }
    });
  }
}
