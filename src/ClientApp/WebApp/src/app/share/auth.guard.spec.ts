import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';
import { AuthGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';

describe('AuthGuard', () => {
  let guard: AuthGuard;
  let router: Router;
  let authService: AuthService;

  beforeEach(() => {
    const authServiceMock = {
      isAuthenticated: jest.fn()
    };
    const routerMock = {
      parseUrl: jest.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        AuthGuard,
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    });

    guard = TestBed.inject(AuthGuard);
    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
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
    (authService.isAuthenticated as jest.Mock).mockReturnValue(true);
    const route = {} as ActivatedRouteSnapshot;
    const state = { url: '/dashboard' } as RouterStateSnapshot;

    const result = guard.canActivate(route, state);

    expect(result).toBe(true);
    expect(authService.isAuthenticated).toHaveBeenCalled();
  });

  it('should redirect to login when user is not authenticated', () => {
    (authService.isAuthenticated as jest.Mock).mockReturnValue(false);
    const loginUrl = {} as UrlTree;
    (router.parseUrl as jest.Mock).mockReturnValue(loginUrl);

    const route = {} as ActivatedRouteSnapshot;
    const state = { url: '/dashboard' } as RouterStateSnapshot;

    const result = guard.canActivate(route, state);

    expect(result).toBe(loginUrl);
    expect(router.parseUrl).toHaveBeenCalledWith('/login');
    expect(authService.isAuthenticated).toHaveBeenCalled();
  });

  it('should handle canActivateChild the same as canActivate', () => {
    (authService.isAuthenticated as jest.Mock).mockReturnValue(true);
    const route = {} as ActivatedRouteSnapshot;
    const state = { url: '/dashboard' } as RouterStateSnapshot;

    const result = guard.canActivateChild(route, state);

    expect(result).toBe(true);
  });
});

