import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
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
export class HomeComponent {
  private readonly oidcSecurityService = inject(OidcSecurityService);
  readonly isAuthenticated$ = this.oidcSecurityService.isAuthenticated$;
}
