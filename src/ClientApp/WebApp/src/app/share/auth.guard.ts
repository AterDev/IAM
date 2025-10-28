import { Injectable, inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { OidcAuthService } from '../services/oidc-auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  private router = inject(Router);
  private auth = inject(OidcAuthService);

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot): boolean | UrlTree {
    const url = state.url;

    if (url.startsWith('/index')) {
      return true;
    }
    
    if (this.auth.isAuthenticated()) {
      return true;
    }
    
    return this.router.parseUrl('/login');
  }
  
  canActivateChild(
    next: ActivatedRouteSnapshot,
    state: RouterStateSnapshot): boolean | UrlTree {
    return this.canActivate(next, state);
  }
}
