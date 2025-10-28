import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';
import { AuthGuard } from './auth.guard';
import { OidcAuthService } from '../services/oidc-auth.service';

describe('AuthGuard', () => {
  let guard: AuthGuard;
  let router: Router;
  let authService: jasmine.SpyObj<OidcAuthService>;

  beforeEach(() => {
    const authServiceSpy = jasmine.createSpyObj('OidcAuthService', ['isAuthenticated']);
    const routerSpy = jasmine.createSpyObj('Router', ['parseUrl']);

    TestBed.configureTestingModule({
      providers: [
        AuthGuard,
        { provide: OidcAuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });

    guard = TestBed.inject(AuthGuard);
    authService = TestBed.inject(OidcAuthService) as jasmine.SpyObj<OidcAuthService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  it('should be created', () => {
    expect(guard).toBeTruthy();
  });

  it('should allow access to /index route without authentication', () => {
    const route = {} as ActivatedRouteSnapshot;
    const state = { url: '/index' } as RouterStateSnapshot;

    const result = guard.canActivate(route, state);

    expect(result).toBe(true);
  });

  it('should allow access when user is authenticated', () => {
    authService.isAuthenticated.and.returnValue(true);
    const route = {} as ActivatedRouteSnapshot;
    const state = { url: '/dashboard' } as RouterStateSnapshot;

    const result = guard.canActivate(route, state);

    expect(result).toBe(true);
    expect(authService.isAuthenticated).toHaveBeenCalled();
  });

  it('should redirect to login when user is not authenticated', () => {
    authService.isAuthenticated.and.returnValue(false);
    const loginUrl = {} as UrlTree;
    (router.parseUrl as jasmine.Spy).and.returnValue(loginUrl);
    
    const route = {} as ActivatedRouteSnapshot;
    const state = { url: '/dashboard' } as RouterStateSnapshot;

    const result = guard.canActivate(route, state);

    expect(result).toBe(loginUrl);
    expect(router.parseUrl).toHaveBeenCalledWith('/login');
    expect(authService.isAuthenticated).toHaveBeenCalled();
  });

  it('should handle canActivateChild the same as canActivate', () => {
    authService.isAuthenticated.and.returnValue(true);
    const route = {} as ActivatedRouteSnapshot;
    const state = { url: '/dashboard' } as RouterStateSnapshot;

    const result = guard.canActivateChild(route, state);

    expect(result).toBe(true);
  });
});
