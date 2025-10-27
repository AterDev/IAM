import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HTTP_INTERCEPTORS, HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthHttpInterceptor } from './auth-http.interceptor';
import { AuthService } from '../../services/auth.service';

describe('AuthHttpInterceptor', () => {
  let httpMock: HttpTestingController;
  let httpClient: HttpClient;
  let authService: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;
  let snackBar: jasmine.SpyObj<MatSnackBar>;

  beforeEach(() => {
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['logout']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigateByUrl']);
    const snackBarSpy = jasmine.createSpyObj('MatSnackBar', ['open']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        {
          provide: HTTP_INTERCEPTORS,
          useClass: AuthHttpInterceptor,
          multi: true,
        },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: MatSnackBar, useValue: snackBarSpy },
      ],
    });

    httpMock = TestBed.inject(HttpTestingController);
    httpClient = TestBed.inject(HttpClient);
    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    snackBar = TestBed.inject(MatSnackBar) as jasmine.SpyObj<MatSnackBar>;
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should add Authorization header when token exists', () => {
    const token = 'test-access-token';
    localStorage.setItem('accessToken', token);

    httpClient.get('/api/test').subscribe();

    const req = httpMock.expectOne('/api/test');
    expect(req.request.headers.has('Authorization')).toBe(true);
    expect(req.request.headers.get('Authorization')).toBe(`Bearer ${token}`);
    req.flush({});
  });

  it('should not add Authorization header when token does not exist', () => {
    httpClient.get('/api/test').subscribe();

    const req = httpMock.expectOne('/api/test');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('should not add Authorization header for excluded URLs', () => {
    const token = 'test-access-token';
    localStorage.setItem('accessToken', token);

    httpClient.get('/connect/token').subscribe();

    const req = httpMock.expectOne('/connect/token');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('should handle 401 error and redirect to login', () => {
    httpClient.get('/api/test').subscribe({
      error: () => { },
    });

    const req = httpMock.expectOne('/api/test');
    req.flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(authService.logout).toHaveBeenCalled();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/login');
    expect(snackBar.open).toHaveBeenCalled();
  });

  it('should handle 403 error with appropriate message', () => {
    httpClient.get('/api/test').subscribe({
      error: () => { },
    });

    const req = httpMock.expectOne('/api/test');
    req.flush(null, { status: 403, statusText: 'Forbidden' });

    expect(snackBar.open).toHaveBeenCalledWith(
      jasmine.stringContaining('403'),
      '了解',
      jasmine.any(Object)
    );
  });

  it('should handle 404 error with custom message', () => {
    const errorDetail = 'Resource not found';
    httpClient.get('/api/test').subscribe({
      error: () => { },
    });

    const req = httpMock.expectOne('/api/test');
    req.flush({ detail: errorDetail }, { status: 404, statusText: 'Not Found' });

    expect(snackBar.open).toHaveBeenCalledWith(
      errorDetail,
      '了解',
      jasmine.any(Object)
    );
  });

  it('should handle 500 error with server message', () => {
    const errorDetail = 'Internal server error';
    const errorTitle = 'Server Error';
    httpClient.get('/api/test').subscribe({
      error: () => { },
    });

    const req = httpMock.expectOne('/api/test');
    req.flush(
      { detail: errorDetail, title: errorTitle },
      { status: 500, statusText: 'Internal Server Error' }
    );

    expect(snackBar.open).toHaveBeenCalledWith(
      jasmine.stringContaining(errorTitle),
      '了解',
      jasmine.any(Object)
    );
  });
});
