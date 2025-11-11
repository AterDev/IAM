import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatChipsModule } from '@angular/material/chips';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatListModule, MatChipsModule],
  templateUrl: './home.component.html'
})
export class HomeComponent {
  private oidcSecurityService = inject(OidcSecurityService);
  isAuthenticated$ = this.oidcSecurityService.isAuthenticated$;
}
